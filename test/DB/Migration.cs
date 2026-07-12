using Interview.DBMigrator;
using Interview.Import;
using Interview.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;


namespace Interview.Test.DB;

[Collection( "FullSetTests" )]
public class Migration( TestStartupFixture Fixture )
{
	[Fact]
	public async Task StartupMigration()
	{
		await Interview.Test.Util.StartMigration();
	}
}
