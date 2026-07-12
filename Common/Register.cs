using Interview.Common.Service;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Common;

public static class GlobalExt
{
	public static IServiceCollection RegisterCommon( this IServiceCollection services )
	{
		services.AddTransient<INotifyMismatch, NotificationService>();
		return services;
	}
}
