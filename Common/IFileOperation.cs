using Interview.Repository.POCO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Common;

public interface IFileOperationRepository
{
	bool CheckHash( string path, string fileName, string hash );
	void Save( FileImport fileImport );
	FileImport Load( string path, string name );

	void Save( TransactionLedger tran );
	void Save( SettlementEntry tran );

	Task<TransactionLedger[]> LoadTransactions( Guid? fileId = null, string path = null, string fileName = null, DateTimeOffset? capturedStart = null, DateTimeOffset? capturedEnd = null );
	Task<SettlementEntry[]> LoadSettlementEntries( Guid? fileId = null, string path = null, string fileName = null, DateTimeOffset? settlementDateStart = null, DateTimeOffset? settlementDateEnd = null );

	Task<TransactionLedger> LoadTransaction( Guid TransactionLedgerId );

	Task<(SettlementEntry[] Settlements, TransactionLedger[] Transactions)> LoadUnreconciled(  );
	Task UpdateSettlement( Guid SettlementEntryId, Guid TransactionLedgerId );
	Task Notify( Guid SettlementEntryId, string Msg );


#if DEBUG
	Task ClearDatabase();
#endif
}
