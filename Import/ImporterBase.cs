// Maybe this was a distinction without a difference, but the idea was to validate the simple data ( just the data file )
// Not all the rules yet, more like parsing for valid, versus business rules

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;

namespace Interview.Import;

internal abstract class ImporterBase<TRecord>
{
	protected ImporterBase(
		Stream data,
		IReadOnlyList<CsvColumnDefinition> columns,
		IConvertCSVDataToObject<TRecord> csvConverter )
	{
		Data = data;
		Columns = columns;
		CsvConverter = csvConverter;
	}

	protected Stream Data { get; }
	protected IReadOnlyList<CsvColumnDefinition> Columns { get; }
	protected IConvertCSVDataToObject<TRecord> CsvConverter { get; }

	private readonly List<InternalImportRowResult<TRecord>> _successfulRows = [];
	private readonly List<InternalImportRowResult<TRecord>> _failedRows = [];

	public IReadOnlyList<InternalImportRowResult<TRecord>> SuccessfulRows => _successfulRows;
	public IReadOnlyList<InternalImportRowResult<TRecord>> FailedRows => _failedRows;

	public virtual async Task Parse()
	{
		Stream csvData = await GetCsvDataStream();

		try
		{
			if( csvData.CanSeek )
			{
				csvData.Seek( 0, SeekOrigin.Begin );
			}

			using StreamReader sr = new( csvData, Encoding.UTF8, true, leaveOpen: true );
			CsvConfiguration configuration = new( CultureInfo.InvariantCulture )
			{
				HasHeaderRecord = true,
				BadDataFound = null,
				MissingFieldFound = null,
				DetectDelimiter = true,
				TrimOptions = TrimOptions.None
			};

			using CsvReader csv = new( sr, configuration );
			if( !await csv.ReadAsync() )
			{
				return;
			}

			csv.ReadHeader();

			while( await csv.ReadAsync() )
			{
				InternalImportRowResult<TRecord> rowResult = ParseRow( csv );
				if( rowResult.ErrorCode == RowImportErrorCode.None )
				{
					_successfulRows.Add( rowResult );
				}
				else
				{
					_failedRows.Add( rowResult );
				}
			}
		}
		finally
		{
			if( DisposeCsvDataStream )
			{
				await csvData.DisposeAsync();
			}
		}
	}

	protected abstract Task<Stream> GetCsvDataStream();

	protected virtual bool DisposeCsvDataStream => false;

	protected virtual void OnAfterRecordConverted( TRecord record, long rowNumber )
	{
	}

	private InternalImportRowResult<TRecord> ParseRow( CsvReader csv )
	{
		List<string> errors = [];
		RowImportErrorCode errorCode = RowImportErrorCode.None;
		Dictionary<string, string?> rawValues = new( StringComparer.OrdinalIgnoreCase );
		Dictionary<string, object?> parsedValues = new( StringComparer.OrdinalIgnoreCase );

		foreach( CsvColumnDefinition definition in Columns )
		{
			if( !TryGetNormalizedFieldValue( csv, definition, out string? normalizedValue, out string? accessError ) )
			{
				errorCode |= RowImportErrorCode.MissingColumn;
				errors.Add( accessError ?? $"Missing column: {definition.Name}" );
				continue;
			}

			if( string.IsNullOrEmpty( normalizedValue ) )
			{
				switch( definition.EmptyBehavior )
				{
					case EmptyValueBehavior.AllowEmpty:
						rawValues[definition.Name] = normalizedValue;
						parsedValues[definition.Name] = null;
						continue;
					case EmptyValueBehavior.ReplaceWithDefault:
						normalizedValue = definition.DefaultValue;
						break;
					case EmptyValueBehavior.Error:
						errorCode |= RowImportErrorCode.MissingValue;
						errors.Add( $"Column '{definition.Name}' is empty." );
						continue;
				}
			}

			rawValues[definition.Name] = normalizedValue;

			if( !definition.AllowHighAscii && HasHighAscii( normalizedValue ) )
			{
				errorCode |= RowImportErrorCode.HighAsciiNotAllowed;
				errors.Add( $"Column '{definition.Name}' contains high-ASCII characters." );
				continue;
			}

			if( !string.IsNullOrWhiteSpace( definition.RegexPattern ) && !Regex.IsMatch( normalizedValue, definition.RegexPattern ) )
			{
				errorCode |= RowImportErrorCode.RegexMismatch;
				errors.Add( $"Column '{definition.Name}' does not match format '{definition.RegexPattern}'." );
				continue;
			}

			if( TryConvertValue( normalizedValue, definition.Type, out object? parsedValue ) )
			{
				parsedValues[definition.Name] = parsedValue;
				continue;
			}

			errorCode |= RowImportErrorCode.InvalidType;
			errors.Add( $"Column '{definition.Name}' value '{normalizedValue}' is not valid for type {definition.Type}." );
		}

		TRecord parsedData = CsvConverter.Convert( rawValues, parsedValues );
		OnAfterRecordConverted( parsedData, csv.Context.Parser.Row );

		return new InternalImportRowResult<TRecord>
		{
			RowNumber = csv.Context.Parser.Row,
			OriginalRow = csv.Context.Parser.RawRecord?.TrimEnd( '\r', '\n' ) ?? string.Empty,
			ParsedData = parsedData,
			ErrorCode = errorCode,
			ErrorMessage = errors.Count == 0 ? null : string.Join( " | ", errors )
		};
	}

	protected static bool TryGetNormalizedFieldValue( CsvReader csv, CsvColumnDefinition definition, out string? value, out string? error )
	{
		value = null;
		error = null;

		try
		{
			string rawValue = csv.GetField( definition.Name ) ?? string.Empty;
			value = definition.TrimWhitespace ? rawValue.Trim() : rawValue;
			return true;
		}
		catch( Exception ex )
		{
			error = $"Column '{definition.Name}' could not be read: {ex.Message}";
			return false;
		}
	}

	protected static bool TryConvertValue( string value, CsvColumnType columnType, out object? convertedValue )
	{
		convertedValue = null;

		switch( columnType )
		{
			case CsvColumnType.String:
				convertedValue = value;
				return true;
			case CsvColumnType.Integer:
				if( int.TryParse( value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue ) )
				{
					convertedValue = intValue;
					return true;
				}

				return false;
			case CsvColumnType.Decimal:
				if( decimal.TryParse( value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalValue ) )
				{
					convertedValue = decimalValue;
					return true;
				}

				return false;
			case CsvColumnType.DateTimeOffset:
				if( DateTimeOffset.TryParse( value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dateTimeOffsetValue ) )
				{
					convertedValue = dateTimeOffsetValue;
					return true;
				}

				return false;
			case CsvColumnType.DateOnly:
				if( DateOnly.TryParse( value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly dateOnlyValue ) )
				{
					convertedValue = dateOnlyValue;
					return true;
				}

				return false;
			default:
				return false;
		}
	}

	protected static bool HasHighAscii( string value ) => value.Any( ch => ch > 127 );
}

internal class InternalImportRowResult<TRecord>
{
	public long RowNumber { get; init; }
	public string OriginalRow { get; init; } = string.Empty;
	public TRecord ParsedData { get; init; } = default!;
	public string? ErrorMessage { get; init; }
	public RowImportErrorCode ErrorCode { get; init; }
}

internal interface IConvertCSVDataToObject<TRecord>
{
	TRecord Convert( IReadOnlyDictionary<string, string?> rawValues, IReadOnlyDictionary<string, object?> parsedValues );
}

internal enum CsvColumnType
{
	String,
	Integer,
	Decimal,
	DateTimeOffset,
	DateOnly
}

internal enum EmptyValueBehavior
{
	AllowEmpty,
	ReplaceWithDefault,
	Error
}

[Flags]
internal enum RowImportErrorCode
{
	None					= 0,
	MissingColumn			= 1,
	MissingValue			= 2,
	RegexMismatch			= 4,
	InvalidType				= 8,
	HighAsciiNotAllowed		= 16
}

internal sealed class CsvColumnDefinition
(
	string name,
	CsvColumnType type,
	bool trimWhitespace,
	EmptyValueBehavior emptyBehavior,
	string regexPattern,
	bool allowHighAscii,
	string? defaultValue = null )
{
	public string Name { get; } = name;
	public CsvColumnType Type { get; } = type;
	public bool TrimWhitespace { get; } = trimWhitespace;
	public EmptyValueBehavior EmptyBehavior { get; } = emptyBehavior;
	public string RegexPattern { get; } = regexPattern;
	public bool AllowHighAscii { get; } = allowHighAscii;
	public string? DefaultValue { get; } = defaultValue;
}
