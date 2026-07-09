using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Interview.Repository;

internal class Util
{
	public static string BuildSysConnectionString( string sourceConn )
	{
		var builder = new DbConnectionStringBuilder { ConnectionString = sourceConn };

		if ( builder.ContainsKey( "Database" ) )
		{
			builder["Database"] = "sys";
		}
		else if ( builder.ContainsKey( "Initial Catalog" ) )
		{
			builder["Initial Catalog"] = "sys";
		}
		else
		{
			builder["Database"] = "sys";
		}

		return builder.ConnectionString;
	}
}
