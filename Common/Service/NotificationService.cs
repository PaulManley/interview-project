using Interview.Util.Ext;
using System;
using System.Collections.Generic;
using System.Text;
using P = Interview.Repository.POCO;

namespace Interview.Common.Service;

public class NotificationService( IFileOperationRepository pRepos) : INotifyMismatch
{
	public void Notify( P.SettlementEntry? S, P.TransactionLedger? T, string Msg )
	{
		Console.WriteLine( $"{Msg} - {T?.RecordNumber}|{T?.MerchantReferenceNo} ==> {S?.RecordNumber}|{S?.NetworkRef}" );

		if ( $"{S?.Id}".ToGuidOrEmpty().IsValid() )
		{
			pRepos.Notify( S.Id, $"{Msg} - {T?.RecordNumber}|{T?.MerchantReferenceNo} ==> {S?.RecordNumber}|{S?.NetworkRef}" );
		}
		
	}
}
