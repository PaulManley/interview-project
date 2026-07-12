using Interview.Common;
using Interview.Common.Service;
using Interview.Import.Reconcile;
using Interview.Repository;
using Interview.DBMigrator;
using Interview.Import;

namespace Portal2;

public class Program
{
	public static async Task Main( string[] args )
	{
		await using var mySqlContainerSetup = new MySqlContainerSetup();
		await mySqlContainerSetup.InitializeAsync("mysql-interview-portal");

		var builder = WebApplication.CreateBuilder(args);

		var services = builder.Services;

		services.AddLogging( x => x.AddConsole() );

		services.SetupRepository()
			.RegisterDBMigrator()
			.RegisterCommon()
			.RegisterPatterns()
			.RegisterImport();



		builder.Services.AddRazorPages();

		var app = builder.Build();

		if ( !app.Environment.IsDevelopment() )
		{
			app.UseExceptionHandler( "/Error" );
		}

		app.UseRouting();

		app.UseAuthorization();

		app.MapStaticAssets();
		app.MapRazorPages()
		   .WithStaticAssets();

		await app.RunAsync();
	}
}
