using Interview.Util.Ext;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Interview.Util;

public class DB
{
	public static string BuildSysConnectionString( string sourceConn, string newDBName = null, string newPassword = null )
	{
		var builder = new DbConnectionStringBuilder { ConnectionString = sourceConn };

		newDBName = newDBName ?? "sys";

		if ( builder.ContainsKey( "Database" ) )
		{
			builder["Database"] = newDBName;
		}
		else if ( builder.ContainsKey( "Initial Catalog" ) )
		{
			builder["Initial Catalog"] = newDBName;
		}
		else
		{
			builder["Database"] = newDBName;
		}
		
		if ( newPassword.IsNotEmpty() )
		{
			if ( builder.ContainsKey( "Password" ) )
			{
				builder["Password"] = newPassword;
			}
		}

		return builder.ConnectionString;
	}
}
