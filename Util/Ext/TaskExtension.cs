using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Interview.Util.Ext;

public static class TaskExtension
{
	public static void AsyncDontWait( this Task task, string taskName = null )
	{
		if ( task == null ) return;
		if ( taskName == null ) taskName = "unnamed task";
		task.ContinueWith( t => MainLogger.Error( $"Non-Waiting-Task:  {taskName}.  Errored in the background.  ", t.Exception ), TaskContinuationOptions.OnlyOnFaulted );
	}

	public static void SafeFireAndForgetStandardException( this Task task, string taskName = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "" )
	{
		string taskLocation = $"File: {filePath}|Member: {memberName}|Line: {lineNumber}";
		if ( task == null ) return;
		if ( taskName == null ) taskName = "unnamed task";
		task.SafeFireAndForget( exc =>
		{
			MainLogger.Error( $"Non-Waiting-Task:  {taskName}.  Errored in the background.  {exc.GetType().Name}.  {exc.Message}.  {taskLocation}", exc );
		}, false );
	}

}
