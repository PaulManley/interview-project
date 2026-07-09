using Interview.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using P = Interview.Repository.POCO;

namespace Interview.Import.Transaction;

public class NormalizeWorkflow( ILogger L, IFileOperationRepository pRepos)
{

	public async Task<Guid> TransactionWorkflow( Stream dataStream, DateTimeOffset fileDate, string path, string fileName, FeeSchedule fs)
	{
		string hashVal = FileInfo.GetSha256Hash(dataStream);

		bool alreadyImported = pRepos.CheckHash( path, fileName, hashVal );
		ZAssert.True( !alreadyImported, $"File has already been imported {fileName}", null, new { path, fileName, hashVal } );

		TranDataImporter t = new TranDataImporter(dataStream, fileDate);
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

		foreach (var rec in combinedRows )
		{
			P.TransactionLedger p = new P.TransactionLedger();

			p.FileImportId = f.Id;
			p.RefTranId = rec.ParsedData.InternalTransactionId;
			p.MerchantId = rec.ParsedData.MerchantId;
			p.MerchantReferenceNo = rec.ParsedData.MerchantRef;
			p.CardType = rec.ParsedData.CardType;
			p.CardLast4 = rec.ParsedData.CardLast4;
			p.GrossAmount = (long)( (rec.ParsedData.GrossAmount??0) * 100.0M );
			p.Currency = rec.ParsedData.Currency;
			p.TranType = rec.ParsedData.Type;
			p.CapturedAt = rec.ParsedData.CapturedAt;

			p.Status = "Imported";
			p.Error = rec.ErrorMessage;
			p.ErrorCode = EnumExtension.UnfoldBitmaskToString<RowImportErrorCode>( (int)rec.ErrorCode );

			var dPercInterchantCents = (decimal)p.GrossAmount * fs.GetFlatAndPercentSafe(p.CardType).Percent;
			var dExpectedInterchangeCents = dPercInterchantCents + fs.GetFlatAndPercentSafe( p.CardType ).FlatCents;

			p.ExpectedInterchangeCents = (int)Math.Round( dExpectedInterchangeCents );	// Might have to look at the rounding math

			var dPerProcessorFee = (decimal)p.GrossAmount * fs.ProcessorMarkup.Percent;
			var dExpectedProcFee = dPerProcessorFee + fs.ProcessorMarkup.FlatCents;

			p.ExpectedProcessorFeeCents = (int)Math.Round( dExpectedProcFee );

			p.ExpectedSettledCents = p.GrossAmount - (p.ExpectedInterchangeCents??0) - (p.ExpectedProcessorFeeCents??0);

			p.RecordNumber = (int)rec.RowNumber;

			pRepos.Save( p );

		}

		return f.Id;



	}

}
