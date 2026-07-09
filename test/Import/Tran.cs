using Interview.Common;
using Interview.Import;
using Interview.Import.Transaction;
using Interview.Repository;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace Interview.Test.Import;

[Collection( "FullSetTests" )]
public class Tran : IDisposable
{
	TestStartupFixture _Fixture = null;
	public Tran( TestStartupFixture Fixture )
	{
		_Fixture = Fixture;
	}


	[Fact]
	public async Task Import_Goofy1_HappyPath()
	{
		await using FileStream fs = new( "./DataSets/goofy1_internal_transactions.csv", FileMode.Open );
		Interview.Import.Transaction.TranDataImporter importer = new( fs, DateTimeOffset.UtcNow );
		await importer.Parse();

		AssertRowError( importer.FailedRows, 2, "gross_amount", RowImportErrorCode.RegexMismatch );
		AssertRowError( importer.FailedRows, 5, "card_last4", RowImportErrorCode.RegexMismatch );
		AssertRowError( importer.FailedRows, 6, "gross_amount", RowImportErrorCode.RegexMismatch );
		AssertRowError( importer.FailedRows, 9, "type", RowImportErrorCode.MissingValue );
		AssertRowError( importer.FailedRows, 11, "card_type", RowImportErrorCode.MissingValue );
		AssertRowError( importer.FailedRows, 13, "currency", RowImportErrorCode.HighAsciiNotAllowed );
	}

	private static void AssertRowError
	(
		IReadOnlyList<InternalImportRowResult<InternalTransactionData>> failedRows,
		long rowNumber,
		string columnName,
		RowImportErrorCode expectedErrorCode )
	{
		InternalImportRowResult<InternalTransactionData> row = Assert.Single( failedRows.Where( r => r.RowNumber == rowNumber ) );
		Assert.Contains( $"{columnName}", row.ErrorMessage, StringComparison.OrdinalIgnoreCase );
		Assert.True( ( expectedErrorCode & row.ErrorCode ) != 0, $"Looking for {row.ErrorCode}" );
	}

	[Fact]
	public async Task Import_InternalTest1_HappyPath()
	{
		await using FileStream fs = new( "./DataSets/test_internal_transactions.csv", FileMode.Open );
		Interview.Import.Transaction.TranDataImporter importer = new( fs, DateTimeOffset.UtcNow );
		await importer.Parse();

		foreach(var rec in importer.FailedRows)
		{
			Console.WriteLine( $"{rec.RowNumber}:  {rec.ErrorMessage} | {rec.ErrorCode}" );
		}

		Assert.True( importer.FailedRows.Count() == 2, "Failed Rows" );

		AssertRowError( importer.FailedRows, 2, "gross_amount", RowImportErrorCode.RegexMismatch );
		AssertRowError( importer.FailedRows, 11, "card_type", RowImportErrorCode.MissingValue );

	}

	[Fact]
	public async Task Import_Official1_HappyPath()
	{
		await using FileStream fs = new( "./DataSets/official_internal_transactions.csv", FileMode.Open );
		Interview.Import.Transaction.TranDataImporter importer = new( fs, DateTimeOffset.UtcNow );
		await importer.Parse();

		foreach ( var rec in importer.FailedRows )
		{
			Console.WriteLine( $"{rec.RowNumber}:  {rec.ErrorMessage} | {rec.ErrorCode}" );
		}

		AssertRowError( importer.FailedRows, 299, "card_type", RowImportErrorCode.MissingValue );
		AssertRowError( importer.FailedRows, 386, "gross_amount", RowImportErrorCode.RegexMismatch );

	}


	[Fact]
	public async Task Import_Goofy1_Normalize1()
	{

		var services = new ServiceCollection();
		services.AddLogging( x => x.AddConsole() );
		services.AddSingleton<IConnectionFactory>( new MySqlConnectionFactory( Interview.Common.Config.ConnectionString ) );
		services.AddSingleton( provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger( nameof( DBMigrator.GlobalSetupMySQL ) ) );
		services.AddTransient<DBMigrator.GlobalSetupMySQL>();
		services.AddTransient<Interview.Common.IFileOperationRepository,FileOperator>();
		
		
		services.AddTransient<NormalizeWorkflow>();
		var provider = services.BuildServiceProvider();

		var db = provider.GetRequiredService<IFileOperationRepository>();
		await db.ClearDatabase();


		string fileNamePath = "./DataSets/goofy1_internal_transactions.csv";
		System.IO.FileInfo fi = new System.IO.FileInfo(fileNamePath);
		string path = fi.DirectoryName;	// TODO:  Make this a relative path to a NFS base path, configurable
		string fileName = fi.Name;

		string pathFeeSchedule = Interview.Common.Config.FeeSchedulePath;
		string feeSchedule = System.IO.File.ReadAllText( pathFeeSchedule );
		FeeSchedule feeScheduleObject = new FeeSchedule(feeSchedule);


		await using FileStream fs = new( fileNamePath, FileMode.Open );

		var wf = provider.GetRequiredService<NormalizeWorkflow>();

		await wf.TransactionWorkflow(fs, DateTimeOffset.UtcNow, path, fileName, feeScheduleObject );

		var transactions = await db.LoadTransactions(path: path, fileName:fileName);

		Assert.True( transactions.SafeCount() == 18 );
	
	}

	[Fact]
	public async Task Import_Test1_Normalize1()
	{

		var services = new ServiceCollection();
		services.AddLogging( x => x.AddConsole() );
		services.AddSingleton<IConnectionFactory>( new MySqlConnectionFactory( Interview.Common.Config.ConnectionString ) );
		services.AddSingleton( provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger( nameof( DBMigrator.GlobalSetupMySQL ) ) );
		services.AddTransient<DBMigrator.GlobalSetupMySQL>();
		services.AddTransient<Interview.Common.IFileOperationRepository, FileOperator>();


		services.AddTransient<NormalizeWorkflow>();
		var provider = services.BuildServiceProvider();

		var db = provider.GetRequiredService<IFileOperationRepository>();
		await db.ClearDatabase();


		string fileNamePath = "./DataSets/test_internal_transactions.csv";
		System.IO.FileInfo fi = new System.IO.FileInfo(fileNamePath);
		string path = fi.DirectoryName; // TODO:  Make this a relative path to a NFS base path, configurable
		string fileName = fi.Name;

		string pathFeeSchedule = Interview.Common.Config.FeeSchedulePath;
		string feeSchedule = System.IO.File.ReadAllText( pathFeeSchedule );
		FeeSchedule feeScheduleObject = new FeeSchedule(feeSchedule);


		await using FileStream fs = new( fileNamePath, FileMode.Open );

		var wf = provider.GetRequiredService<NormalizeWorkflow>();

		await wf.TransactionWorkflow( fs, DateTimeOffset.UtcNow, path, fileName, feeScheduleObject );

		var transactions = await db.LoadTransactions(path: path, fileName:fileName);

		Assert.True( transactions.SafeCount() == 18 );

	}

	[Fact]
	public async Task Import_Full_Normalize1()
	{

		var services = new ServiceCollection();
		services.AddLogging( x => x.AddConsole() );
		services.AddSingleton<IConnectionFactory>( new MySqlConnectionFactory( Interview.Common.Config.ConnectionString ) );
		services.AddSingleton( provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger( nameof( DBMigrator.GlobalSetupMySQL ) ) );
		services.AddTransient<DBMigrator.GlobalSetupMySQL>();
		services.AddTransient<Interview.Common.IFileOperationRepository, FileOperator>();


		services.AddTransient<NormalizeWorkflow>();
		var provider = services.BuildServiceProvider();

		var db = provider.GetRequiredService<IFileOperationRepository>();
		await db.ClearDatabase();


		string fileNamePath = "./DataSets/official_internal_transactions.csv";
		System.IO.FileInfo fi = new System.IO.FileInfo(fileNamePath);
		string path = fi.DirectoryName; // TODO:  Make this a relative path to a NFS base path, configurable
		string fileName = fi.Name;

		string pathFeeSchedule = Interview.Common.Config.FeeSchedulePath;
		string feeSchedule = System.IO.File.ReadAllText( pathFeeSchedule );
		FeeSchedule feeScheduleObject = new FeeSchedule(feeSchedule);


		await using FileStream fs = new( fileNamePath, FileMode.Open );

		var wf = provider.GetRequiredService<NormalizeWorkflow>();

		await wf.TransactionWorkflow( fs, DateTimeOffset.UtcNow, path, fileName, feeScheduleObject );

		var transactions = await db.LoadTransactions(path: path, fileName:fileName);

		Assert.True( transactions.SafeCount() == 546 );

	}

	public void Dispose()
	{
		Console.WriteLine( "Tran::Dispose" );
	}
}
