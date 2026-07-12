using Interview.Common;
using P = Interview.Repository.POCO;
using Interview.Repository.POCO;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Import.Reconcile;


public interface IMatchDataSettlement
{
	int Sequence { get; }
	Task DataMatch( DateTimeOffset? dtStartSettlementEntry, DateTimeOffset? dtEndSettlementEntry );
}

public interface IMatchDataSettlementPostProcessing
{
	int Sequence { get; }
	Task DataMatch( DateTimeOffset? dtStartSettlementEntry, DateTimeOffset? dtEndSettlementEntry );
}

public interface IMatchPatternSettlement
{
	Guid? FindMatch( P.SettlementEntry settlement, P.TransactionLedger[] transactions );
	int Sequence { get; }
}

public class Pattern_NoOp : IMatchPatternSettlement
{
	public int Sequence => 10;

	public Guid? FindMatch( P.SettlementEntry settlement, P.TransactionLedger[] transactions )
	{
		return null;
	}
}

public class Pattern_Full( ILogger L ) : IMatchPatternSettlement
{
	public int Sequence => 10;

	public Guid? FindMatch( SettlementEntry settlement, TransactionLedger[] transactions )
	{
		var matches = transactions
			.Where( x => x.MerchantId.IsEqual( settlement.MerchantId ) )
			.Where( x => x.CardType.IsEqual( settlement.CardType ) )
			.Where( x => x.CardLast4.IsEqual( settlement.CardLast4 ) )
			.Where( x => settlement.SettlementDate.IsDateTimeValid() )
			.Where( x => x.CapturedAt.IsDateTimeValid() )
			.Where( x => settlement.SettlementDate >= x.CapturedAt )
			.Where( x => settlement.SettlementDate.Value.Subtract(x.CapturedAt.Value).TotalDays < 4  )
			.Where( x => x.GrossAmount == settlement.ExpectedGrossOriginalCents )
			.Where( x => x.ExpectedInterchangeCents == settlement.InterchangeFeeCents )
			.Where( x => x.ExpectedSettledCents == settlement.SettledAmountCents )
			.Where( x => x.ExpectedProcessorFeeCents == settlement.ProcessorFeeCents )
			.Where ( x => x.MerchantReferenceNo.IsEqual(settlement.MerchantRef))
			.ToSafeArray();

		foreach ( var m in matches )
		{
			L.LogDebug( "Good Match:  {RecordNumberT}|{MerchantReferenceNo} ==> {RecordNumberS}|{NetworkRef}", m.RecordNumber, m.MerchantReferenceNo, settlement.RecordNumber, settlement.NetworkRef );
		}

		return matches.FirstOrDefault()?.Id;
	}
}

public class Pattern_FullButLate( ILogger L ) : IMatchPatternSettlement
{
	public int Sequence => 10;

	public Guid? FindMatch( SettlementEntry settlement, TransactionLedger[] transactions )
	{
		var matches = transactions
			.Where( x => x.MerchantId.IsEqual( settlement.MerchantId ) )
			.Where( x => x.CardType.IsEqual( settlement.CardType ) )
			.Where( x => x.CardLast4.IsEqual( settlement.CardLast4 ) )
			.Where( x => settlement.SettlementDate.IsDateTimeValid() )
			.Where( x => x.CapturedAt.IsDateTimeValid() )
			.Where( x => settlement.SettlementDate >= x.CapturedAt )
			.Where( x => settlement.SettlementDate.Value.Subtract(x.CapturedAt.Value).TotalDays > 3  )
			.Where( x => x.GrossAmount == settlement.ExpectedGrossOriginalCents )
			.Where( x => x.ExpectedInterchangeCents == settlement.InterchangeFeeCents )
			.Where( x => x.ExpectedSettledCents == settlement.SettledAmountCents )
			.Where( x => x.ExpectedProcessorFeeCents == settlement.ProcessorFeeCents )
			.Where ( x => x.MerchantReferenceNo.IsEqual(settlement.MerchantRef))
			.ToSafeArray();

		foreach ( var m in matches )
		{
			L.LogDebug( "Good Match:  {RecordNumberT}|{MerchantReferenceNo} ==> {RecordNumberS}|{NetworkRef}", m.RecordNumber, m.MerchantReferenceNo, settlement.RecordNumber, settlement.NetworkRef );
		}

		return matches.FirstOrDefault()?.Id;
	}
}

public class Pattern_1PennyOff( INotifyMismatch pNotify ) : IMatchPatternSettlement
{
	public int Sequence => 50;

	public Guid? FindMatch( SettlementEntry settlement, TransactionLedger[] transactions )
	{
		if ( settlement.TransactionLedgerId.IsValid() )
			return null;


		var matches = transactions
			.Where( x => x.MerchantId.IsEqual( settlement.MerchantId ) )
			.Where( x => x.CardType.IsEqual( settlement.CardType ) )
			.Where( x => x.CardLast4.IsEqual( settlement.CardLast4 ) )
			.Where( x => settlement.SettlementDate >= x.CapturedAt )
			.Where( x => DiffWithWiggle
			(
					(x.GrossAmount??0 + x.ExpectedInterchangeCents??0 + x.ExpectedSettledCents??0 + x.ExpectedProcessorFeeCents??0) ,
					( settlement.ExpectedGrossOriginalCents??0 + settlement.InterchangeFeeCents??0 + settlement.SettledAmountCents??0 + settlement.ProcessorFeeCents??0) )
			)
			.Where ( x => x.MerchantReferenceNo.IsEqual(settlement.MerchantRef))
			.ToSafeArray();



		foreach ( var m in matches )
		{
			pNotify.Notify( settlement, m, "Check Within a Penny" );
		}

		return matches.FirstOrDefault()?.Id;
	}

	public bool DiffWithWiggle( long item1, long item2, int wiggle = 1 )
	{
		long diff = item1 - item2;
		if ( diff <= wiggle )
			return true;
		return false;
	}
}

public class Pattern_2PennyOff( INotifyMismatch pNotify ) : IMatchPatternSettlement
{
	public int Sequence => 100;

	public Guid? FindMatch( SettlementEntry settlement, TransactionLedger[] transactions )
	{
		if ( settlement.TransactionLedgerId.IsValid() )
			return null;


		var matches = transactions
			.Where( x => x.MerchantId.IsEqual( settlement.MerchantId ) )
			.Where( x => x.CardType.IsEqual( settlement.CardType ) )
			.Where( x => x.CardLast4.IsEqual( settlement.CardLast4 ) )
			.Where( x => settlement.SettlementDate >= x.CapturedAt )
			.Where( x => DiffWithWiggle
			(
					(x.GrossAmount??0 + x.ExpectedInterchangeCents??0 + x.ExpectedSettledCents??0 + x.ExpectedProcessorFeeCents??0) ,
					( settlement.ExpectedGrossOriginalCents??0 + settlement.InterchangeFeeCents??0 + settlement.SettledAmountCents??0 + settlement.ProcessorFeeCents??0),
					2)
			)
			.Where ( x => x.MerchantReferenceNo.IsEqual(settlement.MerchantRef))
			.ToSafeArray();



		foreach ( var m in matches )
		{
			pNotify.Notify( settlement, m, "Check Within 2 Pennies" );
		}

		return matches.FirstOrDefault()?.Id;
	}

	public bool DiffWithWiggle( long item1, long item2, int wiggle = 1 )
	{
		long diff = item1 - item2;
		if ( diff <= wiggle )
			return true;
		return false;
	}
}

public class Pattern_3PennyOff( INotifyMismatch pNotify ) : IMatchPatternSettlement
{
	public int Sequence => 200;

	public Guid? FindMatch( SettlementEntry settlement, TransactionLedger[] transactions )
	{
		if ( settlement.TransactionLedgerId.IsValid() )
			return null;


		var matches = transactions
			.Where( x => x.MerchantId.IsEqual( settlement.MerchantId ) )
			.Where( x => x.CardType.IsEqual( settlement.CardType ) )
			.Where( x => x.CardLast4.IsEqual( settlement.CardLast4 ) )
			.Where( x => settlement.SettlementDate >= x.CapturedAt )
			.Where( x => DiffWithWiggle
			(
					(x.GrossAmount??0 + x.ExpectedInterchangeCents??0 + x.ExpectedSettledCents??0 + x.ExpectedProcessorFeeCents??0) ,
					( settlement.ExpectedGrossOriginalCents??0 + settlement.InterchangeFeeCents??0 + settlement.SettledAmountCents??0 + settlement.ProcessorFeeCents??0),
					3)
			)
			.Where ( x => x.MerchantReferenceNo.IsEqual(settlement.MerchantRef))
			.ToSafeArray();



		foreach ( var m in matches )
		{
			pNotify.Notify( settlement, m, "Check Within 3 Pennies" );
		}

		return matches.FirstOrDefault()?.Id;
	}

	public bool DiffWithWiggle( long item1, long item2, int wiggle = 1 )
	{
		long diff = item1 - item2;
		if ( diff <= wiggle )
			return true;
		return false;
	}
}

public class Pattern_FarOff1( INotifyMismatch pNotify ) : IMatchPatternSettlement
{
	public int Sequence => 300;

	public Guid? FindMatch( SettlementEntry settlement, TransactionLedger[] transactions )
	{
		if ( settlement.TransactionLedgerId.IsValid() )
			return null;


		var matches = transactions
			.Where( x => x.MerchantId.IsEqual( settlement.MerchantId ) )
			.Where( x => x.CardType.IsEqual( settlement.CardType ) )
			.Where( x => x.CardLast4.IsEqual( settlement.CardLast4 ) )
			.Where( x => settlement.SettlementDate >= x.CapturedAt )
			.Where( x => DiffWithWiggle
			(
					(x.GrossAmount??0 + x.ExpectedInterchangeCents??0 + x.ExpectedSettledCents??0 + x.ExpectedProcessorFeeCents??0) ,
					( settlement.ExpectedGrossOriginalCents??0 + settlement.InterchangeFeeCents??0 + settlement.SettledAmountCents??0 + settlement.ProcessorFeeCents??0),
					100000)
			)
			.Where ( x => x.MerchantReferenceNo.IsEqual(settlement.MerchantRef))
			.ToSafeArray();



		foreach ( var m in matches )
		{
			pNotify.Notify( settlement, m, "Amount Way off 1" );
		}

		return matches.FirstOrDefault()?.Id;
	}

	public bool DiffWithWiggle( long item1, long item2, int wiggle = 1 )
	{
		long diff = item1 - item2;
		if ( diff <= wiggle )
			return true;
		return false;
	}
}

public class Pattern_FarOff2( INotifyMismatch pNotify ) : IMatchPatternSettlement
{
	public int Sequence => 350;

	public Guid? FindMatch( SettlementEntry settlement, TransactionLedger[] transactions )
	{
		if ( settlement.TransactionLedgerId.IsValid() )
			return null;


		var matches = transactions
			.Where( x => x.MerchantId.IsEqual( settlement.MerchantId ) )
			.Where( x => x.CardType.IsEqual( settlement.CardType ) )
			.Where( x => x.CardLast4.IsEqual( settlement.CardLast4 ) )
			.Where( x => settlement.SettlementDate >= x.CapturedAt )
			.Where( x => DiffWithWiggle
			(
					(x.GrossAmount??0 + x.ExpectedInterchangeCents??0 + x.ExpectedSettledCents??0 + x.ExpectedProcessorFeeCents??0) ,
					( settlement.ExpectedGrossOriginalCents??0 + settlement.InterchangeFeeCents??0 + settlement.SettledAmountCents??0 + settlement.ProcessorFeeCents??0),
					1000000)
			)
			.Where ( x => x.MerchantReferenceNo.IsEqual(settlement.MerchantRef))
			.ToSafeArray();



		foreach ( var m in matches )
		{
			pNotify.Notify( settlement, m, "Amount Way off 2" );
		}

		return matches.FirstOrDefault()?.Id;
	}

	public bool DiffWithWiggle( long item1, long item2, int wiggle = 1 )
	{
		long diff = item1 - item2;
		if ( diff <= wiggle )
			return true;
		return false;
	}
}

public class Pattern_Off( INotifyMismatch pNotify ) : IMatchPatternSettlement
{
	public int Sequence => 400;

	public Guid? FindMatch( SettlementEntry settlement, TransactionLedger[] transactions )
	{
		if ( settlement.TransactionLedgerId.IsValid() )
			return null;


		var matches = transactions
			.Where( x => x.MerchantId.IsEqual( settlement.MerchantId ) )
			.Where( x => x.CardType.IsEqual( settlement.CardType ) )
			.Where( x => x.CardLast4.IsEqual( settlement.CardLast4 ) )
			.Where( x => settlement.SettlementDate >= x.CapturedAt )
			.Where( x =>
				( x.GrossAmount??0 + x.ExpectedInterchangeCents??0 + x.ExpectedSettledCents??0 + x.ExpectedProcessorFeeCents??0) !=
				( settlement.ExpectedGrossOriginalCents??0 + settlement.InterchangeFeeCents??0 + settlement.SettledAmountCents??0 + settlement.ProcessorFeeCents??0) )
			.Where ( x => x.MerchantReferenceNo.IsEqual(settlement.MerchantRef))
			.ToSafeArray();



		foreach ( var m in matches )
		{
			pNotify.Notify( settlement, m, "Amount Wrong" );
		}

		return matches.FirstOrDefault()?.Id;
	}

	public bool DiffWithWiggle( long item1, long item2, int wiggle = 1 )
	{
		long diff = item1 - item2;
		if ( diff <= wiggle )
			return true;
		return false;
	}
}

public class PreProcessing_MainMatch( IFileOperationRepository pRepos ) : IMatchDataSettlement
{
	public int Sequence => 100;

	public async Task DataMatch( DateTimeOffset? dtStartSettlementEntry, DateTimeOffset? dtEndSettlementEntry )
	{
		int rows = await pRepos.Reconciliation_MainMatching( dtStartSettlementEntry, dtEndSettlementEntry );
		L.Debug( $"Matched {rows}", new { Name = "PreProcessing_MainMatch", Rows = rows } );
	}
}

public class PreProcessing_MatchWithWiggle( IFileOperationRepository pRepos ) : IMatchDataSettlement
{
	public int Sequence => 2000;

	public async Task DataMatch( DateTimeOffset? dtStartSettlementEntry, DateTimeOffset? dtEndSettlementEntry )
	{
		int rows = await pRepos.Reconciliation_MatchingWithWiggle(2,  dtStartSettlementEntry, dtEndSettlementEntry );
		L.Debug( $"Matched {rows}", new { Name = "PreProcessing_MatchWithWiggle", Rows = rows, Wiggle = 2 } );
	}
}

public class PostProcessing_MatchSplit( IFileOperationRepository pRepos ) : IMatchDataSettlement
{
	public int Sequence => 1000;

	public async Task DataMatch( DateTimeOffset? dtStartSettlementEntry, DateTimeOffset? dtEndSettlementEntry )
	{
		int rows = await pRepos.Reconciliation_MatchingSplit( dtStartSettlementEntry, dtEndSettlementEntry );
		L.Debug( $"Matched {rows}", new { Name = "PostProcessing_MatchSplit", Rows = rows } );
	}
}

