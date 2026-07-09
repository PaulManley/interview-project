using LinqToDB;
using LinqToDB.Mapping;
using System;

namespace Interview.Repository.POCO;

[Table( "SettlementEntry" )]
public class SettlementEntry : AAuditBase
{
	[Column( DataType = DataType.Guid, DbType = "char(36)" ), NotNull]
	public Guid FileImportId { get; set; }

	[Column( DataType = DataType.Guid, DbType = "char(36)" ), Nullable]
	public Guid? TransactionLedgerId { get; set; }

	[Column( Length = 64 ), Nullable]
	public string? NetworkRef { get; set; }

	[Column( Length = 64 ), Nullable]
	public string? MerchantRef { get; set; }

	[Column( Length = 64 ), Nullable]
	public string? MerchantId { get; set; }

	[Column( Length = 16 ), Nullable]
	public string? CardType { get; set; }

	[Column( Length = 4 ), Nullable]
	public string? CardLast4 { get; set; }

	[Column, Nullable]
	public long? SettledAmountCents { get; set; }

	[Column, Nullable]
	public long? InterchangeFeeCents { get; set; }

	[Column, Nullable]
	public long? ProcessorFeeCents { get; set; }

	[Column( Length = 3, DbType = "char(3)" ), Nullable]
	public string? Currency { get; set; }

	[Column, Nullable]
	public DateTimeOffset? SettlementDate { get; set; }

	[Column, Nullable]
	public long? ExpectedGrossOriginalCents { get; set; }

	[Column( Length = 32 ), Nullable]
	public string? Status { get; set; }

	[Column( Length = 256 ), Nullable]
	public string? Error { get; set; }

	[Column( Length = 64 ), Nullable]
	public string? ErrorCode { get; set; }

	[Column, Nullable]
	public int? RecordNumber { get; set; }
	[Column( Length = 256 ), Nullable]
	public string? Notification { get; set; }
}
