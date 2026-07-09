using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Util.Ext;

public static class DateTimeExtension
{
	public static bool IsDateTimeValid( this string sdt )
	{

		return sdt.ToDateTime().IsDateTimeValid();
	}

	public static bool IsDateTimeValid( this DateTimeOffset dt )
	{
		if ( dt == null ) return false;
		if ( dt == default ) return false;
		if ( dt == DateTimeOffset.MinValue ) return false;
		if ( dt == DateTimeOffset.MaxValue ) return false;
		if ( dt.Year == 1 || dt.Year == 0 ) return false;

		return true;
	}

	public static bool IsDateTimeValid( this DateTimeOffset? dt )
	{
		if ( dt == null ) return false;
		if ( !dt.HasValue ) return false;
		if ( dt.HasValue )
		{
			if ( dt.Value == default ) return false;
			if ( dt.Value == DateTimeOffset.MinValue ) return false;
			if ( dt.Value == DateTimeOffset.MaxValue ) return false;
			if ( dt.Value.Year == 1 || dt.Value.Year == 0 ) return false;
		}

		return true;
	}

	public static bool IsDateTimeValid( this DateTime dt )
	{
		if ( dt == null ) return false;
		if ( dt == DateTime.MinValue ) return false;
		if ( dt == DateTime.MaxValue ) return false;
		if ( dt.Year == 1 || dt.Year == 0 ) return false;

		return true;
	}

	public static bool IsDateTimeValid( this DateTime? dt )
	{
		if ( dt == null ) return false;
		if ( !dt.HasValue ) return false;
		if ( dt.HasValue )
		{
			if ( dt.Value == DateTime.MinValue ) return false;
			if ( dt.Value == DateTime.MaxValue ) return false;
			if ( dt.Value.Year == 1 || dt.Value.Year == 0 ) return false;
		}

		return true;
	}


	public static DateTimeOffset? ToDateTimeEx( this object? value )
	{
		if ( value == null ) return null;
		if ( value.GetType() == typeof( DateTimeOffset ) ) return (DateTimeOffset)value;
		if ( value.GetType() == typeof( DateTimeOffset? ) ) return (DateTimeOffset?)value;
		if ( value.GetType() == typeof( DateTime ) ) return (DateTime)value;
		if ( value.GetType() == typeof( DateTime? ) ) return (DateTime?)value;
		string s = value.ToString();
		if ( DateTimeOffset.TryParse( s, out var result1 ) )
		{
			return result1;
		}
		if ( DateTime.TryParse( s, out var result2 ) )
		{
			return result2;
		}
		return null;

	}


	public static DateTime? ToDateTime( this object? value )
	{
		if ( value == null ) return null;
		if ( value.GetType() == typeof( DateTime ) ) return (DateTime)value;
		if ( value.GetType() == typeof( DateTime? ) ) return (DateTime?)value;
		string s = value.ToString();
		if ( DateTime.TryParse( s, out var result ) )
		{
			return result;
		}
		return null;

	}
}
