using System;
using System.Collections.Generic;
using System.Globalization;
using Interview.Import;

namespace Interview.Import.Transaction;

internal class TranDataImporter : ImporterBase<InternalTransactionData>
{
	public const string TypeName = "TranLedger";
	public TranDataImporter( Stream data, DateTimeOffset fileDate )
		: this( data, TransactionImportDefaults.CreateColumns( fileDate ), new ConvertTransactionCsvDataToObject() )
	{
	}

	public TranDataImporter(
		Stream data,
		IReadOnlyList<CsvColumnDefinition> internalTransactionColumns,
		IConvertCSVDataToObject<InternalTransactionData> csvConverter )
		: base( data, internalTransactionColumns, csvConverter )
	{
	}

	protected override Task<Stream> GetCsvDataStream() => Task.FromResult( Data );

	/*
	private static IReadOnlyList<InternalTransactionImportRowResult> MapRows( IReadOnlyList<InternalImportRowResult<InternalTransactionData>> rows )
	{
		List<InternalTransactionImportRowResult> mappedRows = [];

		foreach( InternalImportRowResult<InternalTransactionData> row in rows )
		{
			mappedRows.Add( new InternalTransactionImportRowResult
			{
				RowNumber = row.RowNumber,
				OriginalRow = row.OriginalRow,
				ParsedData = row.ParsedData,
				ErrorCode = row.ErrorCode,
				ErrorMessage = row.ErrorMessage
			} );
		}

		return mappedRows;
	}
	*/
}

internal static class TransactionImportDefaults
{
	public static IReadOnlyList<CsvColumnDefinition> CreateColumns( DateTimeOffset fileDate ) =>
	[
		new( "internal_txn_id", CsvColumnType.String, true, EmptyValueBehavior.Error, "^TXN-[A-Z0-9-]+$", false ),
		new( "merchant_id", CsvColumnType.String, true, EmptyValueBehavior.Error, "^MERCH-\\d{3}$", false ),
		new( "merchant_ref", CsvColumnType.String, true, EmptyValueBehavior.Error, "^ORD-[A-Z0-9-]+$", false ),
		new( "card_type", CsvColumnType.String, true, EmptyValueBehavior.Error, "^(VISA|MASTERCARD|AMEX|DISCOVER)$", false ),
		new( "card_last4", CsvColumnType.String, true, EmptyValueBehavior.Error, "^\\d{4}$", false ),
		new( "gross_amount", CsvColumnType.Decimal, true, EmptyValueBehavior.Error, "^-?\\d+(\\.\\d{1,2})?$", false ),
		new( "currency", CsvColumnType.String, true, EmptyValueBehavior.Error, "^[A-Z]{3}$", false ),
		new( "type", CsvColumnType.String, true, EmptyValueBehavior.Error, "^(SALE|REFUND)$", false ),
		new( "captured_at", CsvColumnType.DateTimeOffset, true, EmptyValueBehavior.ReplaceWithDefault, string.Empty, false, fileDate.ToString( "O", CultureInfo.InvariantCulture ) )
	];
}

internal sealed class ConvertTransactionCsvDataToObject : IConvertCSVDataToObject<InternalTransactionData>
{
	public InternalTransactionData Convert( IReadOnlyDictionary<string, string?> rawValues, IReadOnlyDictionary<string, object?> parsedValues )
	{
		return new InternalTransactionData
		{
			InternalTransactionId = parsedValues.GetValueOrDefault( "internal_txn_id" ) as string,
			MerchantId = parsedValues.GetValueOrDefault( "merchant_id" ) as string,
			MerchantRef = parsedValues.GetValueOrDefault( "merchant_ref" ) as string,
			CardType = parsedValues.GetValueOrDefault( "card_type" ) as string,
			CardLast4 = parsedValues.GetValueOrDefault( "card_last4" ) as string,
			GrossAmount = parsedValues.GetValueOrDefault( "gross_amount" ) as decimal?,
			Currency = parsedValues.GetValueOrDefault( "currency" ) as string,
			Type = parsedValues.GetValueOrDefault( "type" ) as string,
			CapturedAt = parsedValues.GetValueOrDefault( "captured_at" ) as DateTimeOffset?
		};
	}
}



internal sealed class InternalTransactionData
{
	public string? InternalTransactionId { get; init; }
	public string? MerchantId { get; init; }
	public string? MerchantRef { get; init; }
	public string? CardType { get; init; }
	public string? CardLast4 { get; init; }
	public decimal? GrossAmount { get; init; }
	public string? Currency { get; init; }
	public string? Type { get; init; }
	public DateTimeOffset? CapturedAt { get; init; }
}



internal sealed class InternalTransactionImportRowResult : InternalImportRowResult<InternalTransactionData>
{
}

