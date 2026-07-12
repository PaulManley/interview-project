using Interview.Common;
using Interview.Repository.POCO;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using System.Xml.Linq;

namespace Interview.Repository;

/*
I don't have enough time to bulk/batch stream data in and out.

Also, these should all be Async with Cancellation tokens passed through.

I'd also have a generic repository that makes Save/Select standard
*/

public class FileOperator( IConnectionFactory Conn, ILogger L ) : IFileOperationRepository
{
	public bool CheckHash( string path, string fileName, string hash )
	{
		using var db = Conn.Create();

		bool existsByPathAndName = db.GetTable<FileImport>()
			.Any( x => x.FilePath == path && x.FileName == fileName );

		if ( existsByPathAndName )
		{
			return true;
		}

		return db.GetTable<FileImport>().Any( x => x.FileHash == hash );
	}

	public void Save( FileImport fileImport )
	{
		using var db = Conn.Create();

		bool exists = db.GetTable<FileImport>().Any( x => x.Id == fileImport.Id );
		if ( !exists )
		{
			db.Insert( fileImport );
			return;
		}

		db.Update( fileImport );
	}

	public FileImport Load( string path, string name )
	{
		using var db = Conn.Create();

		return db.GetTable<FileImport>().FirstOrDefault( x => x.FilePath == path && x.FileName == name );
	}

	public async Task<TransactionLedger> LoadTransaction( Guid TransactionLedgerId )
	{
		using var db = Conn.Create();

		return db.GetTable<TransactionLedger>().FirstOrDefault( x => x.Id == TransactionLedgerId );
	}

	public async Task<TransactionLedger[]> LoadTransactions(Guid? fileId = null, string path = null, string fileName = null, DateTimeOffset? capturedStart = null, DateTimeOffset? capturedEnd = null )
	{
		/*This is chatty, I might do this fully in sproc*/
		using var db = Conn.Create();
		FileImport? fi = null;

		if ( fileId.IsValid() )
		{
			fi = db.GetTable<FileImport>().FirstOrDefault( x => x.Id == fileId.Value );
		}
		
		if ( fi == null && fileName.IsNotEmpty() )
		{
			fi = db.GetTable<FileImport>().FirstOrDefault( x => x.FilePath == path && x.FileName == fileName );
		}

		if ( fi != null )
			fileId = fi.Id;

		IQueryable<TransactionLedger> query = db.GetTable<TransactionLedger>();

		if ( fileId.IsValid() )
			query = query.Where( x => x.FileImportId == fileId.Value );
		if ( capturedStart.IsDateTimeValid() )
		{
			query = query.Where( x => capturedStart <= x.CapturedAt  );
		}
		if ( capturedEnd.IsDateTimeValid() )
		{
			query = query.Where( x => capturedEnd >= x.CapturedAt );
		}

		query = query.OrderByDescending( x => x.Created );
		query = query.Take(1000);

		var ret = await query.ToArrayAsync();

		return ret.ToSafeArray();
	}

	public async Task<SettlementEntry[]> LoadSettlementEntries( Guid? fileId = null, string path = null, string fileName = null, DateTimeOffset? settlementDateStart = null, DateTimeOffset? settlementDateEnd = null )
	{
		/*This is chatty, I might do this fully in sproc*/

		using var db = Conn.Create();
		FileImport? fi = null;

		if ( fileId.IsValid() )
		{
			fi = db.GetTable<FileImport>().FirstOrDefault( x => x.Id == fileId.Value );
		}

		if ( fi == null && fileName.IsNotEmpty() )
		{
			fi = db.GetTable<FileImport>().FirstOrDefault( x => x.FilePath == path && x.FileName == fileName );
		}

		if ( fi != null )
			fileId = fi.Id;

		IQueryable<SettlementEntry> query = db.GetTable<SettlementEntry>();

		if ( fileId.IsValid() )
			query = query.Where( x => x.FileImportId == fileId.Value );
		if ( settlementDateStart.IsDateTimeValid() )
		{
			query = query.Where( x => settlementDateStart <= x.SettlementDate );
		}
		if ( settlementDateEnd.IsDateTimeValid() )
		{
			query = query.Where( x => settlementDateEnd >= x.SettlementDate );
		}

		query = query.OrderByDescending( x => x.Created );
		query = query.Take( 1000 );

		var ret = await query.ToArrayAsync();

		return ret.ToSafeArray();
	}

	public async Task UpdateSettlement(Guid SettlementEntryId, Guid TransactionLedgerId)
	{
		ZAssert.True( SettlementEntryId.IsValid(), "Settlement Entry Id is not valid" );
		ZAssert.True( TransactionLedgerId.IsValid(), "Transaction Ledger Id is not valid" );

		using var db = Conn.Create();
		int rowsAffected = await db.GetTable<SettlementEntry>()
			.Where( x => x.Id == SettlementEntryId )
			.Set( t => t.TransactionLedgerId, TransactionLedgerId )
			.Set( t => t.Status ,"Match")
			.UpdateAsync();

		ZAssert.True( rowsAffected == 1, "Incorrect number of rows updated",structuredObject: new { rowsAffected } );

	}


	public async Task<(SettlementEntry[] Settlements, TransactionLedger[] Transactions)> LoadUnreconciled( DateTimeOffset? settlementDateStart = null, DateTimeOffset? settlementDateEnd = null  )
	{
		using var db = Conn.Create();

		string queryGetTrans =
"""
SELECT T.*
	FROM TransactionLedger T
		LEFT OUTER JOIN SettlementEntry S ON ( S.TransactionLedgerId = T.Id )
	WHERE 1=1
		AND T.Status = 'Imported'
		AND S.Id IS NULL;
""";

		var retSE = await db.GetTable<SettlementEntry>().Where( x => x.Status == "Imported" ).Where(x => x.TransactionLedgerId == null).ToArrayAsync();
		var retTL = await db.QueryAsync<TransactionLedger>(queryGetTrans);

		return (retSE.ToSafeArray(), retTL.ToSafeArray());
	}

	public async Task<int> Reconciliation_MainMatching( DateTimeOffset? settlementDateStart = null, DateTimeOffset? settlementDateEnd = null )
	{
		using var db = Conn.Create();

		try
		{
			List<DataParameter> prams = new List<DataParameter>();
			prams.Add( new DataParameter( "dtStart", settlementDateStart.IsDateTimeValid() ? settlementDateStart.Value.Date : null ) { DataType = DataType.Date } );
			prams.Add( new DataParameter( "dtEnd", settlementDateEnd.IsDateTimeValid() ? settlementDateStart.Value.Date : null ) { DataType = DataType.Date } );

			var rowsUpdated = await db.ExecuteProcAsync
			(
				"Reconciliation_DirectMatch",
				prams.ToArray()
			);

			return rowsUpdated;
		}
		catch(Exception exc)
		{
			exc.AddParam( "Sproc", "Reconciliation_MainMatching" );
			exc.AddParam( nameof( settlementDateStart ), $"{settlementDateStart}" );	// For non-trivial calls I often add the parameters on the call upward to the exception
			exc.AddParam( nameof( settlementDateEnd ), $"{settlementDateEnd}" );
			throw;
		}
	}

	public async Task<int> Reconciliation_MatchingWithWiggle( int WiggleAmount = 2, DateTimeOffset? settlementDateStart = null, DateTimeOffset? settlementDateEnd = null )
	{
		using var db = Conn.Create();

		try
		{
			List<DataParameter> prams = new List<DataParameter>();
			prams.Add( new DataParameter( "dtStart", settlementDateStart.IsDateTimeValid() ? settlementDateStart.Value.Date : null ) { DataType = DataType.Date } );
			prams.Add( new DataParameter( "dtEnd", settlementDateEnd.IsDateTimeValid() ? settlementDateStart.Value.Date : null ) { DataType = DataType.Date } );
			prams.Add( new DataParameter( "WiggleAmount", WiggleAmount ) { DataType = DataType.Int32 } );

			var rowsUpdated = await db.ExecuteProcAsync
			(
				"Reconciliation_MatchWithWiggleRoom",
				prams.ToArray()
			);

			return rowsUpdated;
		}
		catch ( Exception exc )
		{
			exc.AddParam( "Sproc", "Reconciliation_MatchWithWiggleRoom" );
			exc.AddParam( nameof( settlementDateStart ), $"{settlementDateStart}" );    // For non-trivial calls I often add the parameters on the call upward to the exception
			exc.AddParam( nameof( settlementDateEnd ), $"{settlementDateEnd}" );
			throw;
		}
	}

	public async Task<int> Reconciliation_MatchingSplit( DateTimeOffset? settlementDateStart = null, DateTimeOffset? settlementDateEnd = null )
	{
		using var db = Conn.Create();

		try
		{
			List<DataParameter> prams = new List<DataParameter>();
			prams.Add( new DataParameter( "dtStart", settlementDateStart.IsDateTimeValid() ? settlementDateStart.Value.Date : null ) { DataType = DataType.Date } );
			prams.Add( new DataParameter( "dtEnd", settlementDateEnd.IsDateTimeValid() ? settlementDateStart.Value.Date : null ) { DataType = DataType.Date } );

			var rowsUpdated = await db.ExecuteProcAsync
			(
				"Reconciliation_MatchSplitSettlement",
				prams.ToArray()
			);

			return rowsUpdated;
		}
		catch ( Exception exc )
		{
			exc.AddParam( "Sproc", "Reconciliation_MatchSplitSettlement" );
			exc.AddParam( nameof( settlementDateStart ), $"{settlementDateStart}" );    // For non-trivial calls I often add the parameters on the call upward to the exception
			exc.AddParam( nameof( settlementDateEnd ), $"{settlementDateEnd}" );
			throw;
		}
	}

	public void Save( TransactionLedger tran )
	{
		using var db = Conn.Create();

		bool exists = db.GetTable<TransactionLedger>().Any( x => x.Id == tran.Id );
		if ( !exists )
		{
			db.Insert( tran );
			return;
		}

		db.Update( tran );
	}

	public void Save( SettlementEntry tran )
	{
		using var db = Conn.Create();

		bool exists = db.GetTable<SettlementEntry>().Any( x => x.Id == tran.Id );
		if ( !exists )
		{
			db.Insert( tran );
			return;
		}

		db.Update( tran );
	}

	public async Task Notify( Guid SettlementEntryId, string Msg )
	{
		using var db = Conn.Create();

		await db.GetTable<SettlementEntry>()
			.Where( x => x.Id == SettlementEntryId )
			.Set( t => t.Notification, Msg )
			.UpdateAsync();
	}

	public async Task<TransactionLedger[]> LoadTransactionBySettlementId( Guid SettlementId )
	{
		ZAssert.True( SettlementId.IsValid(), "LoadTransactionBySettlementId by Id has an invalid SettlementId" );

		using var db = Conn.Create();

		SettlementEntry? settleRet = await db.GetTable<SettlementEntry>()
			.Where( x => x.Id == SettlementId )
			.FirstOrDefaultAsync();

		ZAssert.True( settleRet != null, "Settlement Entry could no be loaded" );

		TransactionLedger[] trans = await db.GetTable<TransactionLedger>()
			.Where( x => x.Id == settleRet.TransactionLedgerId )
			.ToArrayAsync();

		return trans;
	}

	public async Task<SettlementEntry> LoadSettlement( Guid SettlementId )
	{
		ZAssert.True( SettlementId.IsValid(), "Repository Load Settlement by Id has an invalid SettlementId" );

		using var db = Conn.Create();

		SettlementEntry? settleRet = await db.GetTable<SettlementEntry>()
			.Where( x => x.Id == SettlementId )
			.FirstOrDefaultAsync();

		return settleRet;
	}



}
