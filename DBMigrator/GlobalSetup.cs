using FluentMigrator.Runner;
using FluentMigrator.Runner.Extensions;
using FluentMigrator.Runner.Exceptions;
using FluentMigrator.Runner.Initialization;
using Interview.DBMigrator.Migration;
using Interview.Repository;
using Interview.Util.Ext;
using LinqToDB.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Interview.DBMigrator;

public static class GlobalExt
{
	public static IServiceCollection RegisterDBMigrator(this IServiceCollection services )
	{
		services.AddSingleton( provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger( nameof( Interview.DBMigrator.GlobalSetupMySQL ) ) );
		services.AddTransient<Interview.DBMigrator.GlobalSetupMySQL>();

		return services;
	}
}

public class GlobalSetupMySQL(ILogger L, IConnectionFactory ConnFactory )
{
	public string[] TAGS = [];

	public void KickOffMigration( string[] args )
	{
		List<string> tags = new List<string>();
		foreach ( string s in args )
		{
			if ( !s.SafeContains( ".exe" ) )
			{
				tags.Add( s );
			}
		}
		TAGS = tags.ToArray();

		using ( var serviceProvider = CreateServices() )
		using ( var scope = serviceProvider.CreateScope() )
		{
			UpdateDatabase( scope.ServiceProvider );
		}
	}

	private ServiceProvider CreateServices()
	{
		L.LogDebug( $"Create Conn String {ConnFactory.MasterConnString}" ); // should not log this

		using var db = ConnFactory.CreateMaster();
		db.Execute( ConnFactory.CreateDBSQL );




		var rr = new ServiceCollection()
			.AddFluentMigratorCore()
			.ConfigureRunner( rb => rb
				.AddMySql()	// bonk, only thing that is mysql related
				.WithGlobalConnectionString( ConnFactory.ConnString )
				.ScanIn( typeof( v20260708_0001_Start ).Assembly ).For.All()
			)
			.AddLogging( lb =>
			{
				lb.SetMinimumLevel( LogLevel.Trace );
				lb.AddConsole();
			} )
			.Configure<RunnerOptions>( opt => { opt.Tags = TAGS; } );

		var ret = rr.BuildServiceProvider( false );
		return ret;

	}

	private void UpdateDatabase( IServiceProvider serviceProvider)
	{
		var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

		try
		{
			runner.MigrateUp();
			L.LogInformation( $"Creating Interview database Info -- Completed" );
		}
		catch ( MissingMigrationsException exc )
		{
			L.LogWarning( exc.Message );
		}
	}
}
