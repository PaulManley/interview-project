using LinqToDB.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Repository;

public interface IConnectionFactory
{
	DataConnection Create();
	DataConnection CreateMaster();
	string ConnString { get; }
	string MasterConnString { get; }
	string CreateDBSQL { get; }
}
