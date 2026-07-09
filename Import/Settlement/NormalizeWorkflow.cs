using Interview.Common;
using Interview.Import.Transaction;
using System;
using System.Collections.Generic;
using System.Text;
using P = Interview.Repository.POCO;

namespace Interview.Import.Settlement;

public class NormalizeWorkflow( ILogger L, IFileOperationRepository pRepos )
{
	public async Task<Guid> TransactionWorkflow( Stream data, DateTimeOffset fileDate, string path, string fileName, FeeSchedule fs )
	{
		string hashVal = FileInfo.GetSha256Hash(data);

		bool alreadyImported = pRepos.CheckHash( path, fileName, hashVal );
		ZAssert.True( !alreadyImported, $"File has already been imported {fileName}", null, new { path, fileName, hashVal } );

		SettleDataImporter t = new SettleDataImporter(data);
		await t.Parse();

		int totalRows = t.FailedRows.SafeCount() + t.SuccessfulRows.SafeCount();

		P.FileImport f = new P.FileImport();
		f.FileName = fileName;
		f.FileHash = hashVal;
		f.FilePath = path;
		f.FileType = TranDataImporter.TypeName;
		f.RecordCount = totalRows;

		L.LogDebug( $"File:  {fileName}.  Count:  {f.RecordCount}" );

		pRepos.Save( f );

		var combinedRows = t.SuccessfulRows.Concat( t.FailedRows ).ToSafeArray();

		foreach ( var rec in combinedRows )
		{
			P.SettlementEntry p = new P.SettlementEntry();

			// If in quarantined state or error state or if gross amount is zero should we compute expected values?

			p.FileImportId = f.Id;
			p.NetworkRef = rec.ParsedData.NetworkRef;
			p.MerchantRef = rec.ParsedData.MerchantRef;
			p.MerchantId = rec.ParsedData.MerchantId;
			p.CardType = rec.ParsedData.CardType;
			p.CardLast4 = rec.ParsedData.CardLast4;
			p.SettledAmountCents = (long)( ( rec.ParsedData.SettledAmount ?? 0 ) * 100.0M );
			p.InterchangeFeeCents = (long)( rec.ParsedData.InterchangeFee * 100.0M );
			p.ProcessorFeeCents = (long)( rec.ParsedData.ProcessorFee * 100.0M );
			p.Currency = rec.ParsedData.Currency;
			p.SettlementDate = rec.ParsedData.SettlementDate?.ToDateTime( TimeOnly.MinValue );

			p.RecordNumber = (int)rec.RowNumber;
			p.Status = "Imported";
			p.Error = rec.ErrorMessage;
			p.ErrorCode = EnumExtension.UnfoldBitmaskToString<RowImportErrorCode>( (int)rec.ErrorCode );

			// Compute the original expected gross amount in cents from settled amount, interchange fee, and processor fee
			p.ExpectedGrossOriginalCents = p.SettledAmountCents + p.InterchangeFeeCents + p.ProcessorFeeCents;

			pRepos.Save( p );

		}

		return f.Id;



	}
}
