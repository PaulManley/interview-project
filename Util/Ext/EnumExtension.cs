using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Util.Ext;

public static class EnumExtension
{
	public static IEnumerable<T> UnfoldBitmask<T>( int bitMask )
	{
		return Enum.GetValues( typeof( T ) ).Cast<int>().Where( m => ( bitMask & m ) > 0 ).Cast<T>();
	}
	public static string[] UnfoldBitmaskToStringArray<T>( int bitMask ) where T : System.Enum
	{
		List<string> ret = new List<string>();
		T[] items = UnfoldBitmask<T>(bitMask).ToArray();
		foreach ( T item in items )
		{
			ret.Add( item.ToString() );
		}

		return ret.ToArray();
	}
	public static string UnfoldBitmaskToString<T>( int bitMask ) where T : System.Enum
	{
		string[] items = UnfoldBitmaskToStringArray<T>(bitMask);
		StringBuilder sb = new StringBuilder();

		return items.JoinSafe();

		
	}
}
