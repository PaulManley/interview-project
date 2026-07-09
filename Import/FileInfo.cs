using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Interview.Import;

internal class FileInfo
{

	public static string GetSha256Hash( Stream stream )
	{
		using var sha256 = SHA256.Create();

		byte[] hash = sha256.ComputeHash(stream);

		stream.Seek( 0, SeekOrigin.Begin );

		return Convert.ToHexString( hash );
	}

}
