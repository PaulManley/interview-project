using Interview.Repository;
using Interview.Repository.POCO;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Test;

internal class Util( IConnectionFactory Conn )
{
	public async Task ClearDatabase()
	{
		using var db = Conn.Create();

		await db.GetTable<SettlementEntry>().DeleteAsync();
		await db.GetTable<TransactionLedger>().DeleteAsync();

		await db.GetTable<FileImport>().DeleteAsync();

	}

	public static async Task StartMigration()
	{
		var services = new ServiceCollection();
		services.AddLogging( x => x.AddConsole() );
		services.AddSingleton<IConnectionFactory>( new MySqlConnectionFactory( Interview.Common.Config.ConnectionString ) );
		services.AddSingleton( provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger( nameof( DBMigrator.GlobalSetupMySQL ) ) );
		services.AddTransient<DBMigrator.GlobalSetupMySQL>();
		var provider = services.BuildServiceProvider();

		var globalSql = provider.GetRequiredService<DBMigrator.GlobalSetupMySQL>();

		globalSql.KickOffMigration( [] );
	}
}
