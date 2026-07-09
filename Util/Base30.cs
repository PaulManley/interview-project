using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Interview.Util;

/*
	Removed 0,1,I,J,L,O
	O & 0 look similar
	1 & I & J & L can look similar in non-serif fonts and depending if in upper or lower case
*/
public static class Base30
{
	private static readonly char[] Base30Chars = "23456789ABCDEFGHKMNPQRSTUVWXYZ".ToCharArray();

	public static string ToBase30String( this Guid data )
	{
		if ( data == null || data == Guid.Empty ) { return string.Empty; }

		BigInteger value = new BigInteger(data.ToByteArray(), isUnsigned: true, isBigEndian: true);   // Convert the byte array to a BigInteger for easier manipulation
		StringBuilder encoded = new StringBuilder();

		// Encode the BigInteger in Base30
		while ( value > 0 )
		{
			value = BigInteger.DivRem( value, 30, out BigInteger remainder );
			encoded.Insert( 0, Base30Chars[(int)remainder] );
		}

		// If the result is empty (in case of an empty or zero-value byte array), return "0"
		return encoded.Length > 0 ? encoded.ToString() : "0";
	}

	public static string ToBase30String( this byte[] data )
	{
		if ( data == null || data.Length == 0 ) { return string.Empty; }

		BigInteger value = new BigInteger(data, isUnsigned: true, isBigEndian: true);   // Convert the byte array to a BigInteger for easier manipulation
		StringBuilder encoded = new StringBuilder();

		// Encode the BigInteger in Base30
		while ( value > 0 )
		{
			value = BigInteger.DivRem( value, 30, out BigInteger remainder );
			encoded.Insert( 0, Base30Chars[(int)remainder] );
		}

		// If the result is empty (in case of an empty or zero-value byte array), return "0"
		return encoded.Length > 0 ? encoded.ToString() : "0";
	}


	public static byte[] FromBase30String( string base30String )
	{
		if ( string.IsNullOrEmpty( base30String ) )
		{
			return Array.Empty<byte>();
		}

		// Convert the Base30 string into a BigInteger
		BigInteger value = BigInteger.Zero;
		foreach ( char c in base30String )
		{
			int digit = Array.IndexOf(Base30Chars,c);
			if ( digit < 0 )
			{
				throw new FormatException( "Invalid character in Base30 string." );
			}
			value = value * 30 + digit;
		}

		// Convert BigInteger to byte array (unsigned, big-endian)
		byte[] byteArray = value.ToByteArray(isUnsigned: true, isBigEndian: true);

		// Remove potential leading zero from BigInteger's internal representation
		if ( byteArray.Length > 1 && byteArray[0] == 0 )
		{
			byteArray = byteArray[1..];
		}

		return byteArray;
	}
}
