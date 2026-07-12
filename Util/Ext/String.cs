using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Util.Ext;

public static partial class StringExtension
{

	public static bool? BOOLN( this string s )
	{
		s = s.TrimSafe();
		if ( s == null ) return null;
		if ( s.IsEqual( "0" ) ) return false;
		if ( s.IsEqual( "1" ) ) return true;
		if ( s.IsEqual( "no" ) ) return false;
		if ( s.IsEqual( "yes" ) ) return true;
		if ( s.IsEqual( "false" ) ) return false;
		if ( s.IsEqual( "on" ) ) return true;
		if ( s.IsEqual( "off" ) ) return false;
		if ( s.IsEqual( "true" ) ) return true;
		if ( s.IsEqual( "success" ) ) return true;  // a little goofy that sometimes we get words
		if ( bool.TryParse( s, out var value ) ) return value;
		return null;

	}

	public static bool BOOL( this string s, bool def = false )
	{
		s = s.TrimSafe();
		if ( s == null ) return def;
		if ( s.IsEqual( "0" ) ) return false;
		if ( s.IsEqual( "1" ) ) return true;
		if ( s.IsEqual( "no" ) ) return false;
		if ( s.IsEqual( "yes" ) ) return true;
		if ( s.IsEqual( "false" ) ) return false;
		if ( s.IsEqual( "true" ) ) return true;
		if ( s.IsEqual( "on" ) ) return true;
		if ( s.IsEqual( "off" ) ) return false;
		if ( s.IsEqual( "success" ) ) return true;  // a little goofy that sometimes we get words
		if ( bool.TryParse( s, out var value ) ) return value;
		return def;

	}

	public static string TrimSafe( this string s )
	{
		if ( s != null )
			return s.Trim();
		return s;
	}

	public static bool SafeContains( this string s, string valueToCompare, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase )
	{
		if ( s == null && valueToCompare == null ) return true;
		if ( s == null && valueToCompare != null ) return false;
		if ( s != null && valueToCompare == null ) return false;

		return s.Contains( valueToCompare, stringComparison );
	}

	public static bool IsEqual( this string s, string valueToCompare, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase )
	{
		return s?.Equals( valueToCompare, stringComparison ) ?? null == valueToCompare;
	}
	public static bool IsNotEqual( this string s, string valueToCompare, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase )
	{
		return !( s?.Equals( valueToCompare, stringComparison ) ?? null == valueToCompare );
	}
	public static string Join( this string[] s )
	{
		if ( s.IsNullOrEmpty() )
			return string.Empty;
		return string.Join( ",", s );
	}

	public static string JoinSafe( this ICollection<string> s, string seperator = "," )
	{
		if ( s == null )
			return string.Empty;
		if ( s.Count() == 0 )
			return string.Empty;
		if ( seperator.IsEmpty() )
			seperator = "";
		return string.Join( seperator, s );
	}

	public static string JoinSafe( this IEnumerable<string> s, string seperator = "," )
	{
		if ( s == null )
			return string.Empty;
		if ( s.Count() == 0 )
			return string.Empty;
		if ( seperator.IsEmpty() )
			seperator = "";
		return string.Join( seperator, s );
	}

	public static bool IsEmpty( this string? s )
	{
		return ( string.IsNullOrWhiteSpace( s ) );
	}

	public static bool IsNotEmpty( this string? s )
	{
		return !( s.IsEmpty() );
	}

	public static string JoinSafe( this string[] s, string seperator = "," )
	{
		if ( s.IsNullOrEmpty() )
			return string.Empty;
		if ( seperator.IsEmpty() )
			seperator = "";
		return string.Join( seperator, s );
	}

	public static bool IsNotNullOrWhiteSpace( this string s )
	{
		return !( string.IsNullOrWhiteSpace( s ) );
	}
	public static bool IsNullOrWhiteSpace( this string s )
	{
		return ( string.IsNullOrWhiteSpace( s ) );
	}

	public static bool SafeContains( this string[] ss, string item )
	{
		if ( ss == null )
			return false;

		foreach ( var s in ss )
		{
			if ( s.Equals( item, StringComparison.OrdinalIgnoreCase ) )
				return true;
		}
		return false;
	}

	public static string Right( this string sValue, int iMaxLength )
	{
		if ( sValue == null )
			return null;

		if ( sValue == string.Empty )
			return sValue;


		if ( sValue.Length > iMaxLength )
		{
			sValue = sValue.Substring( sValue.Length - iMaxLength, iMaxLength );    //Make the string no longer than the max length
		}

		return sValue;
	}

	public static string Left( this string sValue, int iMaxLength )
	{
		if ( sValue == null )
			return null;

		if ( sValue == string.Empty )
			return sValue;

		if ( sValue.Length > iMaxLength )
		{
			sValue = sValue.Substring( 0, iMaxLength );
		}
		return sValue;

	}
}