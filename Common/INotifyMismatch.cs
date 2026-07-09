using System;
using System.Collections.Generic;
using System.Text;
using P = Interview.Repository.POCO; 

namespace Interview.Common;

public interface INotifyMismatch
{
	void Notify( P.SettlementEntry S, P.TransactionLedger T , string Msg);
}
