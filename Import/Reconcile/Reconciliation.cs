using Interview.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using P = Interview.Repository.POCO;

namespace Interview.Import.Reconcile;

/*
 * I would probably make a matching pattern that pre-processed the easy ones of these directly in SQL
*/

public class Reconciliation(IMatchDataSettlement[] PreProcessingMatching,  IMatchPatternSettlement[] PatternsSettlement, IMatchDataSettlementPostProcessing[] PostProcessingMatching, IFileOperationRepository pRepos)
{
	// Change into a step runner

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

	public async Task Process(DateTimeOffset? dtStartSettlementEntry, DateTimeOffset? dtEndSettlementEntry)
	{
		await PreProcessing( dtStartSettlementEntry, dtEndSettlementEntry );

		var unreconciled = await pRepos.LoadUnreconciled( dtStartSettlementEntry, dtEndSettlementEntry );
		await Processing_SlowNSquared( unreconciled .Settlements, unreconciled .Transactions);

		await PostProcessing( dtStartSettlementEntry, dtEndSettlementEntry );
	}

	protected async Task PreProcessing( DateTimeOffset? dtStartSettlementEntry, DateTimeOffset? dtEndSettlementEntry )
	{
		var processList = PreProcessingMatching.ToSafeArray().OrderBy( x => x.Sequence ).ToSafeArray();
		foreach (var pPreProcessing in processList )
		{
			await pPreProcessing.DataMatch( dtStartSettlementEntry , dtEndSettlementEntry );
		}
	}

	protected async Task PostProcessing( DateTimeOffset? dtStartSettlementEntry, DateTimeOffset? dtEndSettlementEntry )
	{
		var processList = PostProcessingMatching.ToSafeArray().OrderBy( x => x.Sequence ).ToSafeArray();
		foreach ( var pPostProcessing in processList )
		{
			await pPostProcessing.DataMatch( dtStartSettlementEntry, dtEndSettlementEntry );
		}
	}

	protected async Task Processing_SlowNSquared( P.SettlementEntry[] Settlements, P.TransactionLedger[] Transactions)
	{
		PatternsSettlement = PatternsSettlement.ToSafeArray().OrderBy( x => x.Sequence ).ToSafeArray();
		var unmatchedTransactions = Transactions.ToList();
		var settlements = Settlements.ToSafeArray();
		var matchedSettlementIds = new HashSet<Guid>();

		foreach (var Pattern in PatternsSettlement)
		{
			var settlementMatches = new List<(P.SettlementEntry Settlement, Guid TransactionId)>();

			foreach(var settlement in settlements)
			{
				if ( settlement.TransactionLedgerId.IsInvalid() && !matchedSettlementIds.Contains(settlement.Id) )
				{
					var id = Pattern.FindMatch( settlement, unmatchedTransactions.ToArray() );
					if ( id.IsValid() )
					{
						settlementMatches.Add( (settlement, id.Value) );
						matchedSettlementIds.Add(settlement.Id);
					}
				}
			}

			// Remove matched transactions and update settlements for this pattern
			foreach(var match in settlementMatches)
			{
				match.Settlement.TransactionLedgerId = match.TransactionId;
				unmatchedTransactions.RemoveAll( x => x.Id == match.TransactionId );
			}
		}

		var updateSettlements = settlements.Where( x => x.TransactionLedgerId.IsValid() ).ToSafeArray();
		foreach(var s in updateSettlements )
		{
			await pRepos.UpdateSettlement(s.Id, s.TransactionLedgerId.Value);
		}

	}


}





public static class MatchPatternSetup
{
	public static IServiceCollection RegisterPatterns( this IServiceCollection services )
	{
		// Register all the processing patterns

		services.Scan( scan => scan
			.FromAssemblyOf<IMatchPatternSettlement>()
			.AddClasses( classes => classes.AssignableTo<IMatchPatternSettlement>() )
			.AsImplementedInterfaces()
			.WithTransientLifetime() );

		services.AddTransient<IMatchPatternSettlement[]>( provider => provider.GetServices<IMatchPatternSettlement>().ToArray() );

		services.Scan( scan => scan
			.FromAssemblyOf<IMatchDataSettlement>()
			.AddClasses( classes => classes.AssignableTo<IMatchPatternSettlement>() )
			.AsImplementedInterfaces()
			.WithTransientLifetime() );

		services.AddTransient<IMatchDataSettlement[]>( provider => provider.GetServices<IMatchDataSettlement>().ToArray() );


		services.Scan( scan => scan
			.FromAssemblyOf<IMatchDataSettlementPostProcessing>()
			.AddClasses( classes => classes.AssignableTo<IMatchPatternSettlement>() )
			.AsImplementedInterfaces()
			.WithTransientLifetime() );

		services.AddTransient<IMatchDataSettlementPostProcessing[]>( provider => provider.GetServices<IMatchDataSettlementPostProcessing>().ToArray() );

		return services;

	}
}

