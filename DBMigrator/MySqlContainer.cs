using Interview.Common;
using Interview.DBMigrator;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MySql;

namespace Interview.Repository;

public sealed class MySqlContainerSetup : IAsyncDisposable
{
	private MySqlContainer _mySqlContainer;

	public async ValueTask InitializeAsync()
	{
		if ( _mySqlContainer is not null )
			return;

		var mysqlDataPath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "InterviewProject", "mysql-data" );
		Directory.CreateDirectory( mysqlDataPath );

		var mysqlRootPassword = Environment.GetEnvironmentVariable( "INTERVIEW_MYSQL_ROOT_PASSWORD" ) ?? "InterviewDevRootPwd1!";

		_mySqlContainer = new MySqlBuilder()
			.WithImage( "mysql:latest" )
			.WithDatabase( "Interview20260708" )
			.WithUsername( "root" )
			.WithPassword( mysqlRootPassword )
			.WithBindMount( mysqlDataPath, "/var/lib/mysql" )
			.Build();

		await _mySqlContainer.StartAsync();

		var connectionString = _mySqlContainer.GetConnectionString();
		if ( !connectionString.Contains( "SslMode", StringComparison.OrdinalIgnoreCase ) )
		{
			connectionString = $"{connectionString};SslMode=None";
		}

		Config.ConnectionString = connectionString;
		Console.WriteLine( "MySqlContainerSetup::MySql container started" );

		var services = new ServiceCollection();
		services.AddLogging( x => x.AddConsole() );
		services.AddSingleton<IConnectionFactory>( new MySqlConnectionFactory( Config.ConnectionString ) );
		services.AddSingleton( provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger( nameof( GlobalSetupMySQL ) ) );
		services.AddTransient<GlobalSetupMySQL>();

		using var provider = services.BuildServiceProvider();
		var globalSql = provider.GetRequiredService<GlobalSetupMySQL>();
		globalSql.KickOffMigration( [] );
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
