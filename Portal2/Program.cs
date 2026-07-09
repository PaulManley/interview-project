using Interview.Common;
using Interview.Common.Service;
using Interview.Import.Reconcile;
using Interview.Repository;

namespace Portal2;

public class Program
{
	public static async Task Main( string[] args )
	{
		await using var mySqlContainerSetup = new MySqlContainerSetup();
		await mySqlContainerSetup.InitializeAsync();

		var builder = WebApplication.CreateBuilder(args);

		var services = builder.Services;

		// There are nicer ways to do this.  Usually I create a "Setup" function in each project that takes in the ServiceCollection and it handles this more internally.
		services.AddLogging( x => x.AddConsole() );
		services.AddSingleton<IConnectionFactory>( new MySqlConnectionFactory( Interview.Common.Config.ConnectionString ) );
		services.AddSingleton( provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger( nameof( Interview.DBMigrator.GlobalSetupMySQL ) ) );
		services.AddTransient<Interview.DBMigrator.GlobalSetupMySQL>();
		services.AddTransient<Interview.Common.IFileOperationRepository, FileOperator>();

		services.AddTransient<Interview.Import.Settlement.NormalizeWorkflow>();
		services.AddTransient<Interview.Import.Transaction.NormalizeWorkflow>();
		MatchPatternSetup.Register( services );
		services.AddTransient<Interview.Import.Reconcile.Reconciliation>();
		services.AddTransient<INotifyMismatch, NotificationService>();

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
