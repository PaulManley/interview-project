using LinqToDB;
using LinqToDB.Mapping;
using System;

namespace Interview.Repository.POCO;

[Table( "TransactionLedger" )]
public class TransactionLedger : AAuditBase
{
	[Column( DataType = DataType.Guid, DbType = "char(36)" ), NotNull]
	public Guid FileImportId { get; set; }

	[Column( Length = 64 ), Nullable]
	public string? RefTranId { get; set; }

	[Column( Length = 64 ), Nullable]
	public string? MerchantId { get; set; }

	[Column( Length = 64 ), Nullable]
	public string? MerchantReferenceNo { get; set; }

	[Column( Length = 16 ), Nullable]
	public string? CardType { get; set; }

	[Column( Length = 4 ), Nullable]
	public string? CardLast4 { get; set; }

	[Column, Nullable]
	public long? GrossAmount { get; set; }

	[Column( Length = 3, DbType = "char(3)" ), Nullable]
	public string? Currency { get; set; }

	[Column( Length = 16 ), Nullable]
	public string? TranType { get; set; }

	[Column, Nullable]
	public DateTimeOffset? CapturedAt { get; set; }



	[Column, Nullable]
	public int? ExpectedInterchangeCents { get; set; }

	[Column, Nullable]
	public int? ExpectedProcessorFeeCents { get; set; }

	[Column, Nullable]
	public long? ExpectedSettledCents { get; set; }


	[Column( Length = 32 ), Nullable]
	public string? Status { get; set; }

	[Column( Length = 256 ), Nullable]
	public string? Error { get; set; }

	[Column( Length = 64 ), Nullable]
	public string? ErrorCode { get; set; }

	[Column, Nullable]
	public int? RecordNumber { get; set; }
}
