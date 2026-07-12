using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentMigrator;
using LinqToDB;

namespace Interview.DBMigrator.Migration;

[Migration( 20260711002, TransactionBehavior.None, "Add Sprocs" )]
public class v20260711_0002_Sprocs(  ) : FluentMigrator.Migration
{
	public override void Down()
	{
		L.Warn( "You should never be downgrading the database" );
	}

	public override void Up()
	{
		L.Trace( "Update DB to version v20260711_0002_Sprocs" );

		string[] sprocList = 
			[
				"Sproc/20260711_Reconciliation_DirectMatch.sql",
				"Sproc/20260711_Reconciliation_MatchWithWiggleRoom.sql"
			];
		

		foreach (string sproc1 in sprocList)
		{
			string resourceName = $"Interview.DBMigrator.{sproc1.Replace( '/', '.' ).Replace( '\\', '.' )}";

			using Stream? stream = typeof( v20260711_0002_Sprocs ).Assembly.GetManifestResourceStream( resourceName );
			if ( stream is null )
				throw new InvalidOperationException( $"Embedded resource not found: {resourceName}" );

			using StreamReader reader = new StreamReader( stream );
			string sql = reader.ReadToEnd();

			if ( sql.IsEmpty() )
				throw new InvalidOperationException( $"Embedded resource found, butt empty: {resourceName}" );

			Execute.Sql( sql );
		}

		
	}
}
