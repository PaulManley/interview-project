using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Repository;

public static class Register
{
	public static IServiceCollection SetupRepository( this IServiceCollection x)
	{
		x.AddSingleton<IConnectionFactory>( new MySqlConnectionFactory( Interview.Common.Config.ConnectionString ) );
		x.AddTransient<Interview.Common.IFileOperationRepository, FileOperator>();

		return x;
	}
}
