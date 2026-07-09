using Interview.Util.Ext;
using System;
using System.Collections.Generic;
using System.Text;

namespace Interview.Util;

public class ZAssert
{
	protected static object ParamsToObject( params object[] oPrams )
	{
		if ( oPrams == null || oPrams.Length == 0 )
		{
			return new System.Dynamic.ExpandoObject();
		}

		var expando = new System.Dynamic.ExpandoObject();
		var dict = (IDictionary<string, object>)expando;

		static void AddUnique( IDictionary<string, object> d, string baseKey, object value )
		{
			if ( string.IsNullOrWhiteSpace( baseKey ) )
				baseKey = "Item";

			var key = baseKey;
			int suffix = 1;
			while ( d.ContainsKey( key ) )
			{
				key = $"{baseKey}_{suffix++}";
			}
			d[key] = value;
		}

		for ( int i = 0; i < oPrams.Length; i++ )
		{
			var item = oPrams[i];

			if ( item is null )
			{
				AddUnique( dict, $"Item{i}", null );
				continue;
			}

			// Merge dictionaries
			if ( item is IDictionary<string, object> idict )
			{
				foreach ( var kv in idict )
				{
					AddUnique( dict, kv.Key, kv.Value );
				}
				continue;
			}

			// Boxed KeyValuePair<string, object>
			var itemType = item.GetType();
			if ( itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof( KeyValuePair<,> ) )
			{
				// attempt to extract key/value where key is string
				var keyProp = itemType.GetProperty("Key");
				var valProp = itemType.GetProperty("Value");
				if ( keyProp != null && valProp != null )
				{
					var keyObj = keyProp.GetValue(item);
					if ( keyObj is string keyStr )
					{
						AddUnique( dict, keyStr, valProp.GetValue( item ) );
						continue;
					}
				}
			}

			// Tuple-like with Item1 and Item2 and Item1 is string
			var p1 = itemType.GetProperty("Item1");
			var p2 = itemType.GetProperty("Item2");
			if ( p1 != null && p2 != null )
			{
				var possibleKey = p1.GetValue(item);
				if ( possibleKey is string keyStr )
				{
					AddUnique( dict, keyStr, p2.GetValue( item ) );
					continue;
				}
			}

			// Default: add as indexed property
			AddUnique( dict, $"Item{i}", item );
		}

		return expando;
	}

	protected static KeyValuePair<string, string>[] ObjectToKVPs( object obj )
	{
		if ( obj == null )
			return Array.Empty<KeyValuePair<string, string>>();

		var result = new List<KeyValuePair<string, string>>();
		var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		var type = obj.GetType();
		const int maxLen = 100;

		// Helper to truncate safely
		static string Truncate( string s, int max )
		{
			if ( s == null ) return string.Empty;
			if ( s.Length <= max ) return s;
			return s.Substring( 0, max );
		}

		// Process properties
		var props = type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public );
		foreach ( var p in props )
		{
			// Skip indexers
			if ( p.GetIndexParameters().Length > 0 )
				continue;

			var name = p.Name;
			if ( !seenNames.Add( name ) )
				continue;

			string valueStr;
			try
			{
				var val = p.GetValue(obj);
				// Convert to string using interpolation as requested
				valueStr = $"{val}";
			}
			catch ( Exception ex )
			{
				// Include exception type to indicate failure reading the value
				valueStr = $"<error: {ex.GetType().Name}>";
			}

			valueStr = Truncate( valueStr, maxLen );
			result.Add( new KeyValuePair<string, string>( name, valueStr ) );
		}

		// Process fields
		var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public );
		foreach ( var f in fields )
		{
			var name = f.Name;

			// Skip compiler-generated backing fields like "<PropertyName>k__BackingField"
			if ( name.StartsWith( "<", StringComparison.Ordinal ) || name.Contains( "k__BackingField" ) )
				continue;

			if ( !seenNames.Add( name ) )
				continue;

			string valueStr;
			try
			{
				var val = f.GetValue(obj);
				valueStr = $"{val}";
			}
			catch ( Exception ex )
			{
				valueStr = $"<error: {ex.GetType().Name}>";
			}

			valueStr = Truncate( valueStr, maxLen );
			result.Add( new KeyValuePair<string, string>( name, valueStr ) );
		}

		return result.ToArray();
	}

	protected static void AddStructuredObjectToException( object obj, Exception exc )
	{
		try
		{
			KeyValuePair<string, string>[] rr = ObjectToKVPs(obj).ToSafeArray();
			foreach ( var kvp in rr )
			{
				exc.AddParam( kvp.Key, kvp.Value );
			}
		}
		catch ( Exception ee )
		{
			Console.WriteLine( ee.ToString() );
		}
	}

	public static void True( bool condition, string userMsg, string internalMsg = null, object structuredObject = null)
	{
		if ( !condition )
		{
			string mI = internalMsg.IsEmpty() ? userMsg : internalMsg;
			mI = mI.IsEmpty() ? "Assertion failed" : mI;
			string mU = userMsg.IsEmpty() ? "Assertion failed" : userMsg;

			var e = new ApplicationException(mI);
			AddStructuredObjectToException( structuredObject, e );
			e.AddParam( "UserMessage", mU );

			throw e;
		}
	}
}
