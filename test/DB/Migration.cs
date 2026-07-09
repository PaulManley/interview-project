using Interview.DBMigrator;
using Interview.Import;
using Interview.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;


namespace Interview.Test.DB;

[Collection( "FullSetTests" )]
public class Migration( TestStartupFixture Fixture )
{
	[Fact]
	public async Task StartupMigration()
	{
		var services = new ServiceCollection();
		services.AddLogging( x => x.AddConsole() );
		services.AddSingleton<IConnectionFactory>(new MySqlConnectionFactory( Interview.Common.Config.ConnectionString ) );
		services.AddSingleton( provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger( nameof( DBMigrator.GlobalSetupMySQL ) ) );
		services.AddTransient<DBMigrator.GlobalSetupMySQL>();
		var provider = services.BuildServiceProvider();

		var globalSql = provider.GetRequiredService<DBMigrator.GlobalSetupMySQL>();

		globalSql.KickOffMigration( [] );
	}
}
