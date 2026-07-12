using Interview.Util.Ext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace Interview.Util;



public class EmptyDisposableSM : IDisposable
{
	public void Dispose()
	{
	}
}

public static class MainLoggerExt
{
	public static void Register(this ILogger ll)
	{
		MainLogger.SetLogger( ll, false );
	}

	public static void Register( this IServiceProvider pCol )
	{
		ILogger pLogger = pCol.GetRequiredService<ILogger>();
		MainLogger.SetLogger( pLogger, false );
	}
}

public class MainLogger : ILoggerProvider,
	Microsoft.Extensions.Logging.ILogger,
	Microsoft.Extensions.Logging.ILoggerFactory
{
	private static MainLogger _This = new MainLogger();
	public static MainLogger This { get => _This; }

	public static bool SkipClassName = false;

	public static void SetLogger( ILoggerFactory ll, bool overrideIfSet = false )
	{
		ILogger lll = ll.CreateLogger( "main" );
		SetLogger( lll, overrideIfSet );
	}

	public static void SetLogger( ILogger l, bool overrideIfSet = false )
	{
		if ( l.GetType() == typeof( MainLogger ) )
		{
			Console.WriteLine( $"Did you just try to MainLogger.SetLogger to itself.... you're going to have a bad time ( recursive )" );
			return;
		}


		bool set = false;
		if ( A == null )
			set = true;
		else if ( overrideIfSet )
			set = true;

		if ( set )
			A = l;
	}


	protected static ILogger A { get; set; }
	
	public static bool LoggerIsSetup()
	{
		bool ret =
		(
			A != null 
		);


		return ret;
	}

	public void AddProvider( ILoggerProvider provider ) { }

	public IDisposable BeginScope<TState>( TState state ) where TState : notnull
	{
		return new EmptyDisposableSM();
	}

	public ILogger CreateLogger( string categoryName )
	{
		return this;
	}

	public void Dispose()
	{
	}

	public bool IsEnabled( LogLevel logLevel )
	{
		if ( logLevel == LogLevel.None ) return false;

		bool enabled = false;

		if ( A != null )
			enabled = enabled || A.IsEnabled( logLevel );


		return enabled;
	}

	public void Log<TState>( LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter )
	{
		if ( formatter == null ) return;
		if ( logLevel == LogLevel.None ) return;

		string message = formatter.Invoke( state, exception );
		WriteToAll( logLevel, message, exception, state, null, null, 0 );
	}


	private static Dictionary<string, object> BuildPayload( string message, Exception exc, object structuredData, string memberName, string filePath, int lineNumber )
	{
		var payload = new Dictionary<string, object>( StringComparer.OrdinalIgnoreCase );

		if ( !string.IsNullOrWhiteSpace( message ) ) payload["Message"] = message;
		if ( !string.IsNullOrWhiteSpace( memberName ) ) payload["MemberName"] = memberName;
		if ( !string.IsNullOrWhiteSpace( filePath ) ) payload["FilePath"] = filePath;
		if ( lineNumber > 0 ) payload["LineNumber"] = lineNumber;

		if ( !SkipClassName )
		{
			string className = string.IsNullOrWhiteSpace( filePath ) ? null : Path.GetFileNameWithoutExtension( filePath );
			if ( !string.IsNullOrWhiteSpace( className ) ) payload["ClassName"] = className;
		}

		if ( exc != null )
		{
			payload["ExceptionType"] = exc.GetType().Name;
			payload["ExceptionMessage"] = exc.Message;
			payload["ExceptionFull"] = exc.ToString();
		}

		MergeStructuredData( payload, structuredData );
		return payload;
	}

	private static void MergeStructuredData( Dictionary<string, object> payload, object structuredData )
	{
		if ( structuredData == null ) return;

		if ( structuredData is IDictionary<string, object> dso )
		{
			foreach ( var kvp in dso )
			{
				if ( string.IsNullOrWhiteSpace( kvp.Key ) ) continue;
				AddPayloadValue( payload, kvp.Key, kvp.Value );
			}
			return;
		}

		if ( structuredData is System.Collections.IDictionary d )
		{
			foreach ( System.Collections.DictionaryEntry item in d )
			{
				string key = $"{item.Key}";
				if ( string.IsNullOrWhiteSpace( key ) ) continue;
				AddPayloadValue( payload, key, item.Value );
			}
			return;
		}

		var props = structuredData.GetType().GetProperties( BindingFlags.Public | BindingFlags.Instance );
		if ( props.Length == 0 )
		{
			AddPayloadValue( payload, "StructuredData", structuredData );
			return;
		}

		for ( int i = 0; i < props.Length; i++ )
		{
			var p = props[i];
			if ( p.GetIndexParameters().Length > 0 ) continue;
			if ( string.IsNullOrWhiteSpace( p.Name ) ) continue;

			object val = null;
			try
			{
				val = p.GetValue( structuredData );
			}
			catch {/*intentional*/}

			AddPayloadValue( payload, p.Name, val );
		}
	}

	private static void AddPayloadValue( Dictionary<string, object> payload, string key, object value )
	{
		if ( string.IsNullOrWhiteSpace( key ) ) return;

		if ( payload.ContainsKey( key ) )
		{
			payload[$"Data_{key}"] = value;
			return;
		}

		payload[key] = value;
	}

	private static string FormatPayload( object state, Exception exception )
	{
		if ( state is IDictionary<string, object> payload )
		{
			if ( payload.TryGetValue( "Message", out var msgObj ) && msgObj != null )
			{
				return $"{msgObj}";
			}
		}

		return $"{state}";
	}

	private static void WriteToAll( LogLevel logLevel, string message, Exception exc, object structuredData, string memberName, string filePath, int lineNumber, bool fatal = false )
	{
		if ( Filtering.ShouldFilter( message ) ) return;

		var payload = BuildPayload( message, exc, structuredData, memberName, filePath, lineNumber );

		if ( A != null && A.IsEnabled( logLevel ) )
		{
			A.Log( logLevel, default, payload, exc, FormatPayload );
		}

		if ( A == null )
		{
			string s = FormatPayload( payload, exc );
			Console.WriteLine(s);
		}
	}

	public static void Error
	(
		string message, object structuredData = null,
		[CallerMemberName]
		string memberName = "",
		[CallerFilePath]
		string filePath = "",
		[CallerLineNumber]
		int lineNumber = 0
	)
	{
		WriteToAll( LogLevel.Error, message, null, structuredData, memberName, filePath, lineNumber );
	}

	public static void Error
	(
		string message, Exception exc, object structuredData = null,
		[CallerMemberName]
		string memberName = "",
		[CallerFilePath]
		string filePath = "",
		[CallerLineNumber]
		int lineNumber = 0
	)
	{
		WriteToAll( LogLevel.Error, message, exc, structuredData, memberName, filePath, lineNumber );
	}

	public static void Error
	(
		Exception exc, object structuredData = null,
		[CallerMemberName]
		string memberName = "",
		[CallerFilePath]
		string filePath = "",
		[CallerLineNumber]
		int lineNumber = 0
	)
	{
		string message = $"{exc?.GetType().Name} - {exc?.Message}";
		WriteToAll( LogLevel.Error, message, exc, structuredData, memberName, filePath, lineNumber );
	}

	public static void Debug
	(
		string message, object structuredData = null,
		[CallerMemberName]
		string memberName = "",
		[CallerFilePath]
		string filePath = "",
		[CallerLineNumber]
		int lineNumber = 0
	)
	{
		WriteToAll( LogLevel.Debug, message, null, structuredData, memberName, filePath, lineNumber );
	}

	public static void Trace
	(
		string message, Exception exc = null, object structuredData = null,
		[CallerMemberName]
		string memberName = "",
		[CallerFilePath]
		string filePath = "",
		[CallerLineNumber]
		int lineNumber = 0
	)
	{
		WriteToAll( LogLevel.Trace, message, exc, structuredData, memberName, filePath, lineNumber );
	}

	public static void Critical
	(
		string message, Exception exc = null, object structuredData = null,
		[CallerMemberName]
		string memberName = "",
		[CallerFilePath]
		string filePath = "",
		[CallerLineNumber]
		int lineNumber = 0
	)
	{
		WriteToAll( LogLevel.Critical, message, exc, structuredData, memberName, filePath, lineNumber );
	}

	public static void Fatal
	(
		string message, object structuredData = null,
		[CallerMemberName]
		string memberName = "",
		[CallerFilePath]
		string filePath = "",
		[CallerLineNumber]
		int lineNumber = 0
	)
	{
		WriteToAll( LogLevel.Critical, message, null, structuredData, memberName, filePath, lineNumber, true );
	}

	public static void Warn
	(
		string message, object structuredData = null,
		[CallerMemberName]
		string memberName = "",
		[CallerFilePath]
		string filePath = "",
		[CallerLineNumber]
		int lineNumber = 0
	)
	{
		WriteToAll( LogLevel.Warning, message, null, structuredData, memberName, filePath, lineNumber );
	}

	public static void Warn
	(
		string message, Exception exc, object structuredData = null,
		[CallerMemberName]
		string memberName = "",
		[CallerFilePath]
		string filePath = "",
		[CallerLineNumber]
		int lineNumber = 0
	)
	{
		WriteToAll( LogLevel.Warning, message, exc, structuredData, memberName, filePath, lineNumber );
	}

	public static void Info
	(
		string message, object structuredData = null,
		[CallerMemberName]
		string memberName = "",
		[CallerFilePath]
		string filePath = "",
		[CallerLineNumber]
		int lineNumber = 0
	)
	{
		WriteToAll( LogLevel.Information, message, null, structuredData, memberName, filePath, lineNumber );
	}




}




public class Filtering
{

	public enum LogFilterMatchType
	{
		Exact,
		StartsWith,
		StartsWithEndsWith,
		StartsWithContains,
		Contains
	}

	public sealed class LogFilterRule
	{
		public LogFilterRule( LogFilterMatchType matchType, string startText, string? endText = null )
		{
			MatchType = matchType;
			StartText = startText;
			NextText = endText;
		}

		public LogFilterMatchType MatchType { get; }
		public string StartText { get; }
		public string? NextText { get; }
	}

	private static readonly LogFilterRule[] _logFilterRules = new[]
	{
		new LogFilterRule( LogFilterMatchType.StartsWith, "Received subscription data for" ),
		new LogFilterRule( LogFilterMatchType.Exact, "MSG" ),
		new LogFilterRule( LogFilterMatchType.Exact, "PING" ),
		new LogFilterRule( LogFilterMatchType.Exact, "PONG" ),
		new LogFilterRule( LogFilterMatchType.StartsWith, "Found subscription handler for" ),
		new LogFilterRule( LogFilterMatchType.StartsWithEndsWith, "PUB ", "(null)" ),
		new LogFilterRule( LogFilterMatchType.StartsWithContains, "PUB C.", "_INBOX." ),
		new LogFilterRule( LogFilterMatchType.Exact, "End subscription MaxMsgs" ),
		new LogFilterRule( LogFilterMatchType.Contains, "Socket.ReceiveAsync" ),
		new LogFilterRule( LogFilterMatchType.Contains, "ProcessUIEventRequest" ),
		new LogFilterRule( LogFilterMatchType.StartsWith, "Found subscription handler for" ),
		new LogFilterRule( LogFilterMatchType.StartsWith,"HMSG trace dump:" ),
		new LogFilterRule( LogFilterMatchType.StartsWith,"UNSUB " ),
		new LogFilterRule( LogFilterMatchType.StartsWith,"HMSG trace parsed:" ),
		new LogFilterRule( LogFilterMatchType.StartsWith,"SUB _INBOX." ),
		new LogFilterRule( LogFilterMatchType.StartsWith,"End subscription Timeout" ),
		new LogFilterRule( LogFilterMatchType.StartsWith,"PUB $JS." ),
		new LogFilterRule( LogFilterMatchType.Exact,"HMSG" ),
		new LogFilterRule( LogFilterMatchType.StartsWith,"Fetch setup maxMsgs:" ),
		new LogFilterRule( LogFilterMatchType.StartsWith,"New subscription _INBOX." ),
		new LogFilterRule( LogFilterMatchType.StartsWith,"Removing subscription _INBOX." ),
		new LogFilterRule( LogFilterMatchType.StartsWith,"PUB WorkQueue." ),
		new LogFilterRule( LogFilterMatchType.Contains, "Socket.SendAsync" ),

	};


	public static bool ShouldFilter( string? message )
	{
		if ( message.IsEmpty() ) return false;

		var trimmed = message.TrimSafe() ?? "";

		foreach ( var rule in _logFilterRules )
		{
			switch ( rule.MatchType )
			{
				case LogFilterMatchType.Exact:
					if ( trimmed.IsEqual( rule.StartText ) ) return true;
					break;
				case LogFilterMatchType.StartsWith:
					if ( trimmed.StartsWith( rule.StartText, StringComparison.OrdinalIgnoreCase ) )
						return true;
					break;
				case LogFilterMatchType.StartsWithEndsWith:
					if ( trimmed.StartsWith( rule.StartText, StringComparison.OrdinalIgnoreCase )
						&& rule.NextText.IsNotEmpty()
						&& trimmed.EndsWith( rule.NextText, StringComparison.OrdinalIgnoreCase ) )
						return true;
					break;
				case LogFilterMatchType.StartsWithContains:
					if ( trimmed.StartsWith( rule.StartText, StringComparison.OrdinalIgnoreCase )
						&& rule.NextText.IsNotEmpty()
						&& trimmed.Contains( rule.NextText, StringComparison.OrdinalIgnoreCase ) )
						return true;
					break;
				case LogFilterMatchType.Contains:
					if ( trimmed.Contains( rule.StartText, StringComparison.OrdinalIgnoreCase ) )
						return true;
					break;
			}
		}

		return false;
	}

}