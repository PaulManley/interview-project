using Interview.Common;
using Interview.DBMigrator;
using LinqToDB.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using Testcontainers.MySql;

namespace Interview.Repository;

public sealed class MySqlContainerSetup : IAsyncDisposable
{
	private MySqlContainer _mySqlContainer;

	public async ValueTask InitializeAsync(string defaultMountPoint = "mysql-data" )
	{
		if ( _mySqlContainer is not null )
			return;

		var mysqlRootPassword = Config.ConnectionPassword;

		if ( !Config.UseDocker )
		{
			Config.ConnectionString = Interview.Util.DB.BuildSysConnectionString( Config.ConnectionString, "Interview20260708", mysqlRootPassword );
			var _connFactory = new MySqlConnectionFactory( Config.ConnectionString );
			using var _db = _connFactory.CreateMaster();
			_db.Execute( _connFactory.CreateDBSQL );
			return;
		}

		L.Info( $"MySqlContainerSetup - Init 1000" );
		var mysqlDataPath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "InterviewProject", defaultMountPoint );
		L.Info( $"MySqlContainerSetup - Init 1010 - {mysqlDataPath}" );
		Console.WriteLine( $"MySqlContainerSetup - Init 1010 - {mysqlDataPath}" );
		Directory.CreateDirectory( mysqlDataPath );

		

		/*Why do this at all?  Because I want to create the database myself since mysql DB setup is case sensitive for column compare, which I think is wrong */
		_mySqlContainer = new MySqlBuilder()
			.WithImage( "mysql:latest" )
			.WithDatabase( "SampleDB" )
			.WithUsername( "root" )
			.WithPassword( mysqlRootPassword )
			.WithBindMount( mysqlDataPath, "/var/lib/mysql" )
			.Build();

		L.Info( $"MySqlContainerSetup - Init 1020" );

		await _mySqlContainer.StartAsync();

		L.Info( $"MySqlContainerSetup - Init 1030" );

		//Interview20260708
		//Util.BuildSysConnectionString( ConnString );

		var connectionString = _mySqlContainer.GetConnectionString();

		Config.ConnectionString = connectionString;

		var connFactory = new MySqlConnectionFactory( connectionString );
		using var db = connFactory.CreateMaster();
		db.Execute( connFactory.CreateDBSQL );

		L.Info( $"MySqlContainerSetup - Init 1050" );

		Config.ConnectionString = Interview.Util.DB.BuildSysConnectionString( connectionString, "Interview20260708" );

		L.Info( $"Created database" );

		var services = new ServiceCollection();
		services.AddLogging( x => x.AddConsole() );
		services.AddSingleton<IConnectionFactory>( new MySqlConnectionFactory( Config.ConnectionString ) );
		services.AddSingleton( provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger( nameof( GlobalSetupMySQL ) ) );
		services.AddTransient<GlobalSetupMySQL>();

		using var provider = services.BuildServiceProvider();
		var globalSql = provider.GetRequiredService<GlobalSetupMySQL>();

		L.Info( $"MySqlContainerSetup - Init 1100" );

		globalSql.KickOffMigration( [] );

		L.Info( $"MySqlContainerSetup - Init 1150" );
	}

	public async ValueTask DisposeAsync()
	{
		if ( _mySqlContainer is null )
			return;

		await _mySqlContainer.DisposeAsync();
		_mySqlContainer = null;
		Console.WriteLine( "MySqlContainerSetup::MySql container disposed" );
	}
}
