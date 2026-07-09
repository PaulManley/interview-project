using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using Interview.Import;

namespace Interview.Import.Settlement;

internal class SettleDataImporter : ImporterBase<ProcessorSettlementRow>
{
	public SettleDataImporter( Stream data )
		: this( data, SettlementImportDefaults.CreateColumns(), new ConvertSettlementCsvDataToObject() )
	{
	}

	public SettleDataImporter(
		Stream data,
		IReadOnlyList<CsvColumnDefinition> settlementColumns,
		IConvertCSVDataToObject<ProcessorSettlementRow> csvConverter )
		: base( data, settlementColumns, csvConverter )
	{
	}

	protected override async Task<Stream> GetCsvDataStream()
	{
		if( Data.CanSeek )
		{
			Data.Seek( 0, SeekOrigin.Begin );
		}

		JsonSerializerOptions options = new()
		{
			PropertyNameCaseInsensitive = true
		};

		List<Dictionary<string, string?>> rows = await JsonSerializer.DeserializeAsync<List<Dictionary<string, string?>>>( Data, options ) ?? [];

		MemoryStream csvStream = new();
		using StreamWriter sw = new( csvStream, Encoding.UTF8, leaveOpen: true );
		using CsvWriter csvWriter = new( sw, CultureInfo.InvariantCulture );

		string[] headers =
		[
			"network_ref",
			"merchant_ref",
			"merchant_id",
			"card_last4",
			"card_type",
			"settled_amount",
			"interchange_fee",
			"processor_fee",
			"currency",
			"settlement_date"
		];

		foreach( string header in headers )
		{
			csvWriter.WriteField( header );
		}

		await csvWriter.NextRecordAsync();

		foreach( Dictionary<string, string?> row in rows )
		{
			foreach( string header in headers )
			{
				row.TryGetValue( header, out string? value );
				csvWriter.WriteField( value ?? string.Empty );
			}

			await csvWriter.NextRecordAsync();
		}

		await sw.FlushAsync();
		csvStream.Seek( 0, SeekOrigin.Begin );
		return csvStream;
	}

	protected override bool DisposeCsvDataStream => true;

	protected override void OnAfterRecordConverted( ProcessorSettlementRow record, long rowNumber )
	{
		record.RecordNumber = (int)rowNumber;
	}
}

internal static class SettlementImportDefaults
{
	public static IReadOnlyList<CsvColumnDefinition> CreateColumns() =>
	[
		new( "network_ref", CsvColumnType.String, true, EmptyValueBehavior.Error, "^ARN[0-9]{20}$", false ),
		new( "merchant_ref", CsvColumnType.String, true, EmptyValueBehavior.Error, "^ORD-[A-Z0-9-]+$", false ),
		new( "merchant_id", CsvColumnType.String, true, EmptyValueBehavior.Error, "^MERCH-\\d{3}$", false ),
		new( "card_last4", CsvColumnType.String, true, EmptyValueBehavior.Error, "^\\d{4}$", false ),
		new( "card_type", CsvColumnType.String, true, EmptyValueBehavior.Error, "^(VISA|MASTERCARD|AMEX|DISCOVER)$", false ),
		new( "settled_amount", CsvColumnType.Decimal, true, EmptyValueBehavior.Error, "^-?\\d+(\\.\\d{1,2})?$", false ),
		new( "interchange_fee", CsvColumnType.Decimal, true, EmptyValueBehavior.Error, "^-?\\d+(\\.\\d{1,2})?$", false ),
		new( "processor_fee", CsvColumnType.Decimal, true, EmptyValueBehavior.Error, "^-?\\d+(\\.\\d{1,2})?$", false ),
		new( "currency", CsvColumnType.String, true, EmptyValueBehavior.Error, "^[A-Z]{3}$", false ),
		new( "settlement_date", CsvColumnType.DateOnly, true, EmptyValueBehavior.Error, "^\\d{4}-\\d{2}-\\d{2}$", false )
	];
}

internal sealed class ConvertSettlementCsvDataToObject : IConvertCSVDataToObject<ProcessorSettlementRow>
{
	public ProcessorSettlementRow Convert( IReadOnlyDictionary<string, string?> rawValues, IReadOnlyDictionary<string, object?> parsedValues )
	{
		return new ProcessorSettlementRow
		{
			NetworkRef = rawValues.GetValueOrDefault( "network_ref" ) ?? string.Empty,
			MerchantRef = rawValues.GetValueOrDefault( "merchant_ref" ) ?? string.Empty,
			MerchantId = rawValues.GetValueOrDefault( "merchant_id" ) ?? string.Empty,
			CardLast4 = rawValues.GetValueOrDefault( "card_last4" ) ?? string.Empty,
			CardType = rawValues.GetValueOrDefault( "card_type" ) ?? string.Empty,
			SettledAmountTXT = rawValues.GetValueOrDefault( "settled_amount" ) ?? string.Empty,
			SettledAmount = parsedValues.GetValueOrDefault( "settled_amount" ) as decimal?,
			InterchangeFeeTXT = rawValues.GetValueOrDefault( "interchange_fee" ) ?? string.Empty,
			InterchangeFee = parsedValues.GetValueOrDefault( "interchange_fee" ) as decimal? ?? 0m,
			ProcessorFeeTXT = rawValues.GetValueOrDefault( "processor_fee" ) ?? string.Empty,
			ProcessorFee = parsedValues.GetValueOrDefault( "processor_fee" ) as decimal? ?? 0m,
			Currency = rawValues.GetValueOrDefault( "currency" ) ?? string.Empty,
			SettlementDateTXT = rawValues.GetValueOrDefault( "settlement_date" ) ?? string.Empty,
			SettlementDate = parsedValues.GetValueOrDefault( "settlement_date" ) as DateOnly?
		};
	}
}

internal sealed class ProcessorSettlementRow
{
	public int RecordNumber { get; set; } = -1;
	public string NetworkRef { get; set; } = string.Empty;
	public string MerchantRef { get; set; } = string.Empty;
	public string MerchantId { get; set; } = string.Empty;
	public string CardLast4 { get; set; } = string.Empty;
	public string CardType { get; set; } = string.Empty;
	public string SettledAmountTXT { get; set; } = string.Empty;
	public decimal? SettledAmount { get; set; }
	public string InterchangeFeeTXT { get; set; } = string.Empty;
	public decimal InterchangeFee { get; set; }
	public string ProcessorFeeTXT { get; set; } = string.Empty;
	public decimal ProcessorFee { get; set; }
	public string Currency { get; set; } = string.Empty;
	public string SettlementDateTXT { get; set; } = string.Empty;
	public DateOnly? SettlementDate { get; set; }
}
