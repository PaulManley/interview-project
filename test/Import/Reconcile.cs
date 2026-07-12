using Interview.Common;
using Interview.Common.Service;
using Interview.Import.Reconcile;
using Interview.Repository;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Test.Import;

[Collection( "FullSetTests" )]
public class Reconcile( TestStartupFixture Fixture )
{
	[Fact]
	public async Task Reconcile_Test_001()
	{
		var services = new ServiceCollection();
		services.AddLogging( x => x.AddConsole() );
		services.AddSingleton<IConnectionFactory>( new MySqlConnectionFactory( Interview.Common.Config.ConnectionString ) );
		services.AddSingleton( provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger( nameof( DBMigrator.GlobalSetupMySQL ) ) );
		services.AddTransient<DBMigrator.GlobalSetupMySQL>();
		services.AddTransient<Interview.Common.IFileOperationRepository, FileOperator>();

		services.AddTransient<Interview.Import.Settlement.NormalizeWorkflow>();
		services.AddTransient<Interview.Import.Transaction.NormalizeWorkflow>();
		MatchPatternSetup.RegisterPatterns( services );
		services.AddTransient<Interview.Import.Reconcile.Reconciliation>();
		services.AddTransient<INotifyMismatch,NotificationService>();
		services.AddTransient<Interview.Test.Util>();

		var provider = services.BuildServiceProvider();
		var db = provider.GetRequiredService<IFileOperationRepository>();

		var clearDB = provider.GetRequiredService<Interview.Test.Util>();
		await Interview.Test.Util.StartMigration();
		await clearDB.ClearDatabase();

		string fileNamePath = "./DataSets/test_processor_settlement.json";
		System.IO.FileInfo fi = new System.IO.FileInfo(fileNamePath);
		string path = fi.DirectoryName; // TODO:  Make this a relative path to a NFS base path, configurable
		string fileName = fi.Name;

		string pathFeeSchedule = Interview.Common.Config.FeeSchedulePath;
		string feeSchedule = System.IO.File.ReadAllText( pathFeeSchedule );
		FeeSchedule feeScheduleObject = new FeeSchedule(feeSchedule);

		await using FileStream fs = new( fileNamePath, FileMode.Open );

		var wfSettlement = provider.GetRequiredService<Interview.Import.Settlement.NormalizeWorkflow>();

		await wfSettlement.TransactionWorkflow( fs, DateTimeOffset.UtcNow, path, fileName, feeScheduleObject );

		var wfTran = provider.GetRequiredService<Interview.Import.Transaction.NormalizeWorkflow>();

		fileNamePath = "./DataSets/test_internal_transactions.csv";
		fi = new System.IO.FileInfo(fileNamePath);
		path = fi.DirectoryName; // TODO:  Make this a relative path to a NFS base path, configurable
		fileName = fi.Name;

		await using FileStream fs2 = new( fileNamePath, FileMode.Open );
		await wfTran.TransactionWorkflow( fs2, DateTimeOffset.UtcNow, path, fileName, feeScheduleObject );


		var reconcileProcess = provider.GetRequiredService<Interview.Import.Reconcile.Reconciliation>();
		await reconcileProcess.Process( null, null);

	}
}
