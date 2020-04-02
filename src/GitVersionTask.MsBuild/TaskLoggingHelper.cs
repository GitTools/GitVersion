using System;
using System.Text;
using Microsoft.Build.Framework;

namespace GitVersionTask.MsBuild
{
    /// <summary>
    /// Copied much of this code from https://github.com/microsoft/msbuild/blob/master/src/Shared/TaskLoggingHelper.cs which is licenced under
    /// the MIT licence.
    /// </summary>
    /// <remarks>In an effort to remove dependency on MsBuild utilities assembly, in order to fix https://github.com/GitTools/GitVersion/issues/2125</remarks>
    public class TaskLoggingHelper
    {
        private ITask _taskInstance;
        private IBuildEngine _buildEngine;

        /// <summary>
        /// public constructor
        /// </summary>
        /// <param name="taskInstance">task containing an instance of this class</param>
        public TaskLoggingHelper(ITask taskInstance)
        {
            _taskInstance = taskInstance ?? throw new ArgumentNullException(nameof(taskInstance));
            TaskName = taskInstance.GetType().Name;
        }

        /// <summary>
        /// Public constructor which can be used by task factories to assist them in logging messages.
        /// </summary>
        public TaskLoggingHelper(IBuildEngine buildEngine, string taskName)
        {
            TaskName = taskName;
            _buildEngine = buildEngine;
        }


        /// <summary>
        /// Gets the name of the parent task.
        /// </summary>
        /// <value>Task name string.</value>
        protected string TaskName { get; }

        /// <summary>
        /// Shortcut property for getting our build engine - we retrieve it from the task instance
        /// </summary>
        protected IBuildEngine BuildEngine
        {
            get
            {
                // If the task instance does not equal null then use its build engine because
                // the task instances build engine can be changed for example during tests. This changing of the engine on the same task object is not expected to happen
                // during normal operation.
                if (_taskInstance != null)
                {
                    return _taskInstance.BuildEngine;
                }

                return _buildEngine;
            }
        }

        public bool HasLoggedErrors { get; private set; }

        private void EnsureBuildEngineInitialised()
        {
            if (BuildEngine == null)
            {
                throw new InvalidOperationException("Cannot log before BuildEngine is initialised");
            }

        }

        /// <summary>
        /// Logs a message using the specified string.
        /// Thread safe.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="messageArgs">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
        public void LogMessage(string message, params object[] messageArgs)
        {
            LogMessage(MessageImportance.Normal, message, messageArgs);
        }

        /// <summary>
        /// Logs a message of the given importance using the specified string.
        /// Thread safe.
        /// </summary>
        /// <remarks>
        /// Take care to order the parameters correctly or the other overload will be called inadvertently.
        /// </remarks>
        /// <param name="importance">The importance level of the message.</param>
        /// <param name="message">The message string.</param>
        /// <param name="messageArgs">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
        public void LogMessage(MessageImportance importance, string message, params object[] messageArgs)
        {

            BuildMessageEventArgs e = new BuildMessageEventArgs
                (
                    message,                             // message
                    null,                                // help keyword
                    TaskName,                            // sender
                    importance,                          // importance
                    DateTime.UtcNow,                     // timestamp
                    messageArgs                          // message arguments
                );

            EnsureBuildEngineInitialised();

            BuildEngine.LogMessageEvent(e);

        }

        /// <summary>
        /// Logs a warning using the specified string.
        /// Thread safe.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="messageArgs">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
        public void LogWarning(string message, params object[] messageArgs)
        {
            LogWarning(null, null, null, null, 0, 0, 0, 0, message, messageArgs);
        }

        /// <summary>
        /// Logs a warning using the specified string and other warning details.
        /// Thread safe.
        /// </summary>
        /// <param name="subcategory">Description of the warning type (can be null).</param>
        /// <param name="warningCode">The warning code (can be null).</param>
        /// <param name="helpKeyword">The help keyword for the host IDE (can be null).</param>
        /// <param name="file">The path to the file causing the warning (can be null).</param>
        /// <param name="lineNumber">The line in the file causing the warning (set to zero if not available).</param>
        /// <param name="columnNumber">The column in the file causing the warning (set to zero if not available).</param>
        /// <param name="endLineNumber">The last line of a range of lines in the file causing the warning (set to zero if not available).</param>
        /// <param name="endColumnNumber">The last column of a range of columns in the file causing the warning (set to zero if not available).</param>
        /// <param name="message">The message string.</param>
        /// <param name="messageArgs">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
        public void LogWarning
        (
            string subcategory,
            string warningCode,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string message,
            params object[] messageArgs
        )
        {
            EnsureBuildEngineInitialised();

            // All of our warnings should have an error code, so the user has something
            // to look up in the documentation. To help find warnings without error codes,
            // temporarily uncomment this line and run the unit tests.
            //if (null == warningCode) File.AppendAllText("c:\\warningsWithoutCodes", message + "\n");
            // We don't have a Debug.Assert for this, because it would be triggered by <Error> and <Warning> tags.

            // If the task has missed out all location information, add the location of the task invocation;
            // that gives the user something.
            bool fillInLocation = (String.IsNullOrEmpty(file) && (lineNumber == 0) && (columnNumber == 0));

            var e = new BuildWarningEventArgs
                (
                    subcategory,
                    warningCode,
                    fillInLocation ? BuildEngine.ProjectFileOfTaskNode : file,
                    fillInLocation ? BuildEngine.LineNumberOfTaskNode : lineNumber,
                    fillInLocation ? BuildEngine.ColumnNumberOfTaskNode : columnNumber,
                    endLineNumber,
                    endColumnNumber,
                    message,
                    helpKeyword,
                    TaskName,
                    DateTime.UtcNow,
                    messageArgs
                );

            BuildEngine.LogWarningEvent(e);
        }

        /// <summary>
        /// Logs a warning using the message from the given exception context.
        /// Thread safe.
        /// </summary>
        /// <param name="exception">Exception to log.</param>
        /// <exception cref="ArgumentNullException">Thrown when <c>exception</c> is null.</exception>
        public void LogWarningFromException(Exception exception)
        {
            LogWarningFromException(exception, false);
        }

        /// <summary>
        /// Logs a warning using the message (and optionally the stack-trace) from the given exception context.
        /// Thread safe.
        /// </summary>
        /// <param name="exception">Exception to log.</param>
        /// <param name="showStackTrace">If true, the exception callstack is appended to the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when <c>exception</c> is null.</exception>
        public void LogWarningFromException(Exception exception, bool showStackTrace)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            string message = exception.Message;

            if (showStackTrace)
            {
                message += Environment.NewLine + exception.StackTrace;
            }

            LogWarning(message);
        }

        /// <summary>
        /// Logs an error using the specified string.
        /// Thread safe.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="messageArgs">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
        public void LogError(string message, params object[] messageArgs)
        {
            LogError(null, null, null, null, 0, 0, 0, 0, message, messageArgs);
        }

        /// <summary>
        /// Logs an error using the specified string and other error details.
        /// Thread safe.
        /// </summary>
        /// <param name="subcategory">Description of the error type (can be null).</param>
        /// <param name="errorCode">The error code (can be null).</param>
        /// <param name="helpKeyword">The help keyword for the host IDE (can be null).</param>
        /// <param name="file">The path to the file containing the error (can be null).</param>
        /// <param name="lineNumber">The line in the file where the error occurs (set to zero if not available).</param>
        /// <param name="columnNumber">The column in the file where the error occurs (set to zero if not available).</param>
        /// <param name="endLineNumber">The last line of a range of lines in the file where the error occurs (set to zero if not available).</param>
        /// <param name="endColumnNumber">The last column of a range of columns in the file where the error occurs (set to zero if not available).</param>
        /// <param name="message">The message string.</param>
        /// <param name="messageArgs">Optional arguments for formatting the message string.</param>
        /// <exception cref="ArgumentNullException">Thrown when <c>message</c> is null.</exception>
        public void LogError
        (
            string subcategory,
            string errorCode,
            string helpKeyword,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string message,
            params object[] messageArgs
        )
        {
            EnsureBuildEngineInitialised();

            // If the task has missed out all location information, add the location of the task invocation;
            // that gives the user something.
            bool fillInLocation = (String.IsNullOrEmpty(file) && (lineNumber == 0) && (columnNumber == 0));

            var e = new BuildErrorEventArgs
                (
                    subcategory,
                    errorCode,
                    fillInLocation ? BuildEngine.ProjectFileOfTaskNode : file,
                    fillInLocation ? BuildEngine.LineNumberOfTaskNode : lineNumber,
                    fillInLocation ? BuildEngine.ColumnNumberOfTaskNode : columnNumber,
                    endLineNumber,
                    endColumnNumber,
                    message,
                    helpKeyword,
                    TaskName,
                    DateTime.UtcNow,
                    messageArgs
                );
            BuildEngine.LogErrorEvent(e);

            HasLoggedErrors = true;
        }

        /// <summary>
        /// Logs an error using the message from the given exception context.
        /// No callstack will be shown.
        /// Thread safe.
        /// </summary>
        /// <param name="exception">Exception to log.</param>
        /// <exception cref="ArgumentNullException">Thrown when <c>e</c> is null.</exception>
        public void LogErrorFromException(Exception exception)
        {
            LogErrorFromException(exception, false);
        }

        /// <summary>
        /// Logs an error using the message (and optionally the stack-trace) from the given exception context.
        /// Thread safe.
        /// </summary>
        /// <param name="exception">Exception to log.</param>
        /// <param name="showStackTrace">If true, callstack will be appended to message.</param>
        /// <exception cref="ArgumentNullException">Thrown when <c>exception</c> is null.</exception>
        public void LogErrorFromException(Exception exception, bool showStackTrace)
        {
            LogErrorFromException(exception, showStackTrace, false, null);
        }

        /// <summary>
        /// Logs an error using the message, and optionally the stack-trace from the given exception, and
        /// optionally inner exceptions too.
        /// Thread safe.
        /// </summary>
        /// <param name="exception">Exception to log.</param>
        /// <param name="showStackTrace">If true, callstack will be appended to message.</param>
        /// <param name="showDetail">Whether to log exception types and any inner exceptions.</param>
        /// <param name="file">File related to the exception, or null if the project file should be logged</param>
        /// <exception cref="ArgumentNullException">Thrown when <c>exception</c> is null.</exception>
        public void LogErrorFromException(Exception exception, bool showStackTrace, bool showDetail, string file)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            string message;

            if (!showDetail && (Environment.GetEnvironmentVariable("MSBUILDDIAGNOSTICS") == null)) // This env var is also used in ToolTask
            {
                message = exception.Message;

                if (showStackTrace)
                {
                    message += Environment.NewLine + exception.StackTrace;
                }
            }
            else
            {
                // The more comprehensive output, showing exception types
                // and inner exceptions
                var builder = new StringBuilder(200);
                do
                {
                    builder.Append(exception.GetType().Name);
                    builder.Append(": ");
                    builder.AppendLine(exception.Message);
                    if (showStackTrace)
                    {
                        builder.AppendLine(exception.StackTrace);
                    }
                    exception = exception.InnerException;
                } while (exception != null);

                message = builder.ToString();
            }

            LogError(null, null, null, file, 0, 0, 0, 0, message);
        }
    }
}
