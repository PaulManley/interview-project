using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Util.Ext;

public static class ArrayExtension
{
	public static bool IsNullOrEmpty<T>( this T[] a )
	{
		if ( a == null ) return true;
		if ( a.Length == 0 ) return true;
		return false;
	}

	public static int SafeCount<TSource>( this IEnumerable<TSource> source )
	{
		if ( source == null ) return 0;
		return source.Count();
	}

	public static T[] ToSafeArray<T>( this IEnumerable<T> source )
	{
		if ( source == null ) return new T[0];
		return source.ToArray();
	}
}
