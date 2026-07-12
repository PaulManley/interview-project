using Interview.Common;
using Interview.Common.Service;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo( "Interview.Test" )]


namespace Interview.Import;

public static class GlobalExt
{
	public static IServiceCollection RegisterImport( this IServiceCollection services )
	{
		services.AddTransient<Interview.Import.Settlement.NormalizeWorkflow>();
		services.AddTransient<Interview.Import.Transaction.NormalizeWorkflow>();
		services.AddTransient<Interview.Import.Reconcile.Reconciliation>();

		return services;
	}
}
