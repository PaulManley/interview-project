using Interview.Common;
using Interview.Repository.POCO;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using P = Interview.Repository.POCO;

namespace Interview.Import.Reconcile;

/*
 * I would probably make a matching pattern that pre-processed the easy ones of these directly in SQL
*/

public class Reconciliation( IMatchPatternSettlement[] PatternsSettlement, IFileOperationRepository pRepos)
{
	/*
	 * Process:
	 *	Grab all the un-reconciled items
	 *		Tran and Settlements
	 *	Exclude Quarantine items
	 *	Pass in IMatchingPattern[]
	 *	{
	 *		Load Data(Settlement, Trans[])
	 *		FindMatch()
	 *	}
	 *	
	 *	foreach(IMatchingPattern p)
	 *	{
	 *		foreach(settlement)
	 *			Match m = FindMatch()
	 *	}
	 *	
	 *	Exclude from next loop
	 */

	public async Task Process( P.SettlementEntry[] Settlements, P.TransactionLedger[] Transactions)
	{
		PatternsSettlement = PatternsSettlement.ToSafeArray().OrderBy( x => x.Sequence ).ToSafeArray();
		var trans = Transactions.ToSafeArray();
		var settlements = Settlements.ToSafeArray();

		foreach (var Pattern in PatternsSettlement)
		{
			foreach(var settlement in settlements)
			{
				if ( settlement.TransactionLedgerId.IsInvalid() )
				{
					var id = Pattern.FindMatch( settlement, trans );
					if ( id.IsValid() )
					{
						settlement.TransactionLedgerId = id;
					}
				}
			}
		}

		var updateSettlements = settlements.Where( x => x.TransactionLedgerId.IsValid() ).ToSafeArray();
		foreach(var s in updateSettlements )
		{
			await pRepos.UpdateSettlement(s.Id, s.TransactionLedgerId.Value);
		}
		
	}


}


public interface IMatchPatternSettlement
{
	Guid? FindMatch( P.SettlementEntry settlement, P.TransactionLedger[] transactions );
	int Sequence { get; }
}

public class Pattern_NoOp : IMatchPatternSettlement
{
	public int Sequence => 10;

	public Guid? FindMatch( SettlementEntry settlement, TransactionLedger[] transactions )
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

		foreach(var m in matches)
		{
			L.LogDebug( "Good Match:  {RecordNumberT}|{MerchantReferenceNo} ==> {RecordNumberS}|{NetworkRef}",m.RecordNumber, m.MerchantReferenceNo, settlement.RecordNumber, settlement.NetworkRef );
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

public class Pattern_1PennyOff( INotifyMismatch pNotify) : IMatchPatternSettlement
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


public class MatchPatternSetup
{
	public static void Register( IServiceCollection services )
	{

		services.Scan( scan => scan
			.FromAssemblyOf<IMatchPatternSettlement>()
			.AddClasses( classes => classes.AssignableTo<IMatchPatternSettlement>() )
			.AsImplementedInterfaces()
			.WithTransientLifetime() );

		// Register the array of all IMatchPatternSettlement implementations
		services.AddTransient<IMatchPatternSettlement[]>( provider => provider.GetServices<IMatchPatternSettlement>().ToArray() );

	}
}