using Interview.Common;
using Interview.Repository.POCO;
using LinqToDB;
using LinqToDB.Async;
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
			.UpdateAsync();

		ZAssert.True( rowsAffected == 1, "Incorrect number of rows updated",structuredObject: new { rowsAffected } );

	}


	public async Task<(SettlementEntry[] Settlements, TransactionLedger[] Transactions)> LoadUnreconciled()
	{
		using var db = Conn.Create();
		var retSE = await db.GetTable<SettlementEntry>().Where( x => x.Status == "Imported" ).ToArrayAsync();
		var retTL = await db.GetTable<TransactionLedger>().Where( x => x.Status == "Imported" ).ToArrayAsync();

		return (retSE.ToSafeArray(), retTL.ToSafeArray());
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


#if DEBUG
	// DELETE ALL THE DATA
	// I would also wrap this in a build param to make it impossible in MINT/Sandbox/Staging/QA/Prod .. IE, check the environment as well and remove from usage.
	// Or put it into the Test project only

	public async Task ClearDatabase()
	{
		using var db = Conn.Create();

		await db.GetTable<SettlementEntry>().DeleteAsync();
		await db.GetTable<TransactionLedger>().DeleteAsync();

		await db.GetTable<FileImport>().DeleteAsync();
		
	}
#endif
}
