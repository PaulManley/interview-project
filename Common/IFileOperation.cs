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
	Task<TransactionLedger[]> LoadTransactionBySettlementId( Guid SettlementId );
	Task<SettlementEntry> LoadSettlement( Guid SettlementId );

	Task<int> Reconciliation_MatchingWithWiggle( int WiggleAmount = 2, DateTimeOffset? settlementDateStart = null, DateTimeOffset? settlementDateEnd = null );
	Task<int> Reconciliation_MainMatching( DateTimeOffset? settlementDateStart = null, DateTimeOffset? settlementDateEnd = null );
	Task<int> Reconciliation_MatchingSplit( DateTimeOffset? settlementDateStart = null, DateTimeOffset? settlementDateEnd = null );

	Task<(SettlementEntry[] Settlements, TransactionLedger[] Transactions)> LoadUnreconciled( DateTimeOffset? settlementDateStart = null, DateTimeOffset? settlementDateEnd = null );
	Task UpdateSettlement( Guid SettlementEntryId, Guid TransactionLedgerId );
	Task Notify( Guid SettlementEntryId, string Msg );

}
