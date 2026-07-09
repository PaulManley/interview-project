using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Util.Ext;

public static class GuidExtensions
{
	// When creating ephemeral identities for system to system, I set the Id to a known invalid number
	public const string         CONST_SYSTEM_USERID                 = "00000000-0000-0000-0000-00000000000f";
	public static readonly Guid CONST_SYSTEM_GUID                   = new Guid("00000000-0000-0000-0000-00000000000f");

	public const string         CONST_SYSTEM_PLATFORMLOGIN_ID       = "00000000-B000-0000-0000-00000000000B";
	public static readonly Guid CONST_SYSTEM_PLATFORMLOGIN_GUID     = new Guid("00000000-B000-0000-0000-00000000000B");

	public static bool IsInvalid( this Guid id )
	{
		return id == Guid.Empty || id == CONST_SYSTEM_GUID || id == CONST_SYSTEM_PLATFORMLOGIN_GUID;
	}

	public static bool IsInvalid( this Guid? id )
	{
		return id == null || id == Guid.Empty || id == CONST_SYSTEM_GUID || id == CONST_SYSTEM_PLATFORMLOGIN_GUID;
	}

	public static bool IsValid( this Guid id )
	{
		return !IsInvalid( id );
	}

	public static bool IsValid( this Guid? id )
	{
		return !IsInvalid( id );
	}

	public static bool IsNullOrDefault( this Guid id )
	{
		return id == Guid.Empty;
	}
	public static bool IsNotNullOrDefault( this Guid id )
	{
		return id != Guid.Empty;
	}

	public static bool IsNullOrDefault( this Guid? id )
	{
		if ( id is null || id == Guid.Empty )
			return true;

		return false;
	}
	public static bool IsNotNullOrDefault( this Guid? id )
	{
		if ( id is null || id == Guid.Empty )
			return false;

		return true;
	}

	public static Guid? ToGuidN( this object? value )
	{
		if ( value == null ) return null;
		if ( value.GetType() == typeof( Guid ) && ( (Guid)value ) == Guid.Empty ) return null;
		if ( value.GetType() == typeof( Guid? ) && ( (Guid?)value ) == Guid.Empty ) return null;
		if ( value.GetType() == typeof( Guid ) ) return (Guid)value;
		if ( value.GetType() == typeof( Guid? ) ) return (Guid?)value;
		//string s = value.ToString();
		string s = $"{value}".Trim();
		if ( Guid.TryParse( s, out var result ) )
		{
			return result;
		}
		return null;

	}

	public static Guid ToGuidOrEmpty( this object? value )
	{
		if ( value == null ) return Guid.Empty;
		if ( value.GetType() == typeof( Guid ) ) return (Guid)value;
		if ( value.GetType() == typeof( Guid? ) )
		{
			Guid? gg = (value as Guid?);
			if ( gg.HasValue )
				return gg.Value;    // Else fall through into parsing
		}
		string s = $"{value}".Trim();
		if ( Guid.TryParse( s, out var result ) )
		{
			return result;
		}
		return Guid.Empty;
	}
}
