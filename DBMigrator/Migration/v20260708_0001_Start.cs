using System;
using System.Collections.Generic;
using System.Text;
using FluentMigrator;
using LinqToDB;

namespace Interview.DBMigrator.Migration;

[Migration( 20260708001, TransactionBehavior.None, "Starting" )]
public class v20260708_0001_Start( ILogger L ) : FluentMigrator.Migration
{
	public override void Down()
	{
		L.LogWarning( "You should never be downgrading the database" );
	}

	public override void Up()
	{
		L.LogInformation( "Update DB to version v20260708_0001_Start" );

		Create.Table( "FileImport" )
			.WithColumn( "Id" ).AsCustom( "char(36)" ).PrimaryKey( "PK_FileImport" )
			.WithColumn( "FilePath" ).AsString( 256 ).NotNullable()
			.WithColumn( "FileHash" ).AsString( 256 ).NotNullable()
			.WithColumn( "FileName" ).AsString( 256 ).NotNullable()
			.WithColumn( "RecordCount" ).AsInt32().NotNullable()
			.WithColumn( "FileType" ).AsString( 256 ).NotNullable()

			// Suggest:  IdentityId, IdentityName
			.WithColumn( "Created" ).AsCustom( "TIMESTAMP" ).NotNullable().WithDefault( SystemMethods.CurrentUTCDateTime );

		Create.Index( "UX_FileImport_FilePath_FileName" )
			.OnTable( "FileImport" )
			.OnColumn( "FilePath" ).Ascending()
			.OnColumn( "FileName" ).Ascending()
			.WithOptions().Unique();

		Create.Index( "UX_FileImport_FileHash" )
			.OnTable( "FileImport" )
			.OnColumn( "FileHash" ).Ascending()
			.WithOptions().Unique();

		Create.Table( "TransactionLedger" )
			.WithColumn( "Id" ).AsCustom( "char(36)" ).PrimaryKey( "PK_TransactionLedger" )
			.WithColumn( "FileImportId" ).AsCustom( "char(36)" ).NotNullable()
			.WithColumn( "RefTranId" ).AsString( 64 ).Nullable()
			.WithColumn( "MerchantId" ).AsString( 64 ).Nullable()
			.WithColumn( "MerchantReferenceNo" ).AsString( 64 ).Nullable()
			.WithColumn( "CardType" ).AsString( 16 ).Nullable()
			.WithColumn( "CardLast4" ).AsString( 4 ).Nullable()
			.WithColumn( "GrossAmount" ).AsInt64().Nullable()
			.WithColumn( "Currency" ).AsCustom( "char(3)" ).Nullable()
			.WithColumn( "TranType" ).AsString( 16 ).Nullable()
			.WithColumn( "RecordNumber" ).AsInt32().Nullable()

			.WithColumn( "ExpectedInterchangeCents" ).AsInt32().Nullable()
			.WithColumn( "ExpectedProcessorFeeCents" ).AsInt32().Nullable()
			.WithColumn( "ExpectedSettledCents" ).AsInt64().Nullable()

			.WithColumn( "Status" ).AsString( 32 ).Nullable()
			.WithColumn( "Error" ).AsString( 256 ).Nullable()
			.WithColumn( "ErrorCode" ).AsString( 64 ).Nullable()

			.WithColumn( "CapturedAt" ).AsCustom( "TIMESTAMP" ).Nullable()
			.WithColumn( "Created" ).AsCustom( "TIMESTAMP" ).NotNullable().WithDefault( SystemMethods.CurrentUTCDateTime );

		Create.Index( "IX_TransactionLedger_FileImportId" )
			.OnTable( "TransactionLedger" )
			.OnColumn( "FileImportId" );

		Create.ForeignKey( "FK_TransactionLedger_FileImportId" )
			.FromTable( "TransactionLedger" ).ForeignColumn( "FileImportId" )
			.ToTable( "FileImport" ).PrimaryColumn( "Id" );

		Create.Table( "SettlementEntry" )
			.WithColumn( "Id" ).AsCustom( "char(36)" ).PrimaryKey( "PK_SettlementEntry" )
			.WithColumn( "FileImportId" ).AsCustom( "char(36)" ).NotNullable()
			.WithColumn( "NetworkRef" ).AsString( 64 ).Nullable()
			.WithColumn( "MerchantRef" ).AsString( 64 ).Nullable()
			.WithColumn( "MerchantId" ).AsString( 64 ).Nullable()
			.WithColumn( "CardType" ).AsString( 16 ).Nullable()
			.WithColumn( "CardLast4" ).AsString( 4 ).Nullable()
			.WithColumn( "SettledAmountCents" ).AsInt64().Nullable()
			.WithColumn( "InterchangeFeeCents" ).AsInt64().Nullable()
			.WithColumn( "ProcessorFeeCents" ).AsInt64().Nullable()
			.WithColumn( "Currency" ).AsCustom( "char(3)" ).Nullable()

			.WithColumn ( "TransactionLedgerId").AsCustom( "char(36)" ).Nullable()

			.WithColumn( "Status" ).AsString( 32 ).Nullable()
			.WithColumn( "Error" ).AsString( 256 ).Nullable()
			.WithColumn( "ErrorCode" ).AsString( 64 ).Nullable()
			.WithColumn( "Notification" ).AsString( 256 ).Nullable()

			.WithColumn( "RecordNumber" ).AsInt32().Nullable()

			.WithColumn( "ExpectedGrossOriginalCents" ).AsInt64().Nullable()

			.WithColumn( "SettlementDate" ).AsDate().Nullable()
			.WithColumn( "Created" ).AsCustom( "TIMESTAMP" ).NotNullable().WithDefault( SystemMethods.CurrentUTCDateTime );

		Create.Index( "IX_SettlementEntry_FileImportId" )
			.OnTable( "SettlementEntry" )
			.OnColumn( "FileImportId" );

		Create.ForeignKey( "FK_SettlementEntry_FileImportId" )
			.FromTable( "SettlementEntry" ).ForeignColumn( "FileImportId" )
			.ToTable( "FileImport" ).PrimaryColumn( "Id" );

		Create.ForeignKey( "FK_SettlementEntry_TransactionLedgerId" )
			.FromTable( "SettlementEntry" ).ForeignColumn( "TransactionLedgerId" )
			.ToTable( "TransactionLedger" ).PrimaryColumn( "Id" );

	}
}
