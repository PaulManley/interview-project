using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Util.Ext;

public static class LogParamExtension
{
	public static void AddParam( this Exception e, string name, string value )
	{
		if ( e == null || e.Data == null || string.IsNullOrWhiteSpace( name ) || string.IsNullOrWhiteSpace( value ) )
		{
			return;
		}
		if ( e.Data.Contains( name ) )
		{
			name = $"{name}_{Guid.NewGuid().ToBase30String().Right( 8 )}";
		}

		e.Data[name] = value;
	}
}
