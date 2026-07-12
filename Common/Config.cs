using Interview.Util.Ext;
using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Common;

public static class Config
{
	public static string ConnectionString { get; set; } = "Server=127.0.0.1;Port=3306;Database=Interview20260708;User ID=root;Password=ISREPLACEDATRUNTIME;SslMode=None;";
	public static string ConnectionPassword { get; set; } = Environment.GetEnvironmentVariable( "Main.INTERVIEW_MYSQL_ROOT_PASSWORD" ) ?? "InterviewDevRootPwd1!";
	public static string FeeSchedulePath { get; set; } = "DataSets/fee_schedule.json";
	public static bool UseDocker { get; set; } = (Environment.GetEnvironmentVariable( "Main.UseDocker" ) ?? "0").BOOL(false);

}

