using System;
using System.Collections.Generic;
using System.Text;
using LinqToDB;
using LinqToDB.Data;

namespace Interview.Repository;

public class MySqlConnectionFactory(string conn) : IConnectionFactory
{
	public DataConnection Create()
	{
		DataConnection DB = new DataConnection( new DataOptions().UseMySql( conn ) );

		return DB;
	}

	public DataConnection CreateMaster()
	{
		DataConnection DB = new DataConnection( new DataOptions().UseMySql( MasterConnString ) );

		return DB;
	}

	public string ConnString => conn;

	public string MasterConnString => Util.BuildSysConnectionString( ConnString );
	public string CreateDBSQL =>
"""
CREATE DATABASE IF NOT EXISTS Interview20260708 CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
""";

}

