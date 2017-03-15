namespace GitVersion
{
    using System;
    using GitTools.Logging;

    /// <summary>
    /// Wraps the <see cref="Logger" /> for use by GitTools.
    /// </summary>
    public class LoggerWrapper : ILogProvider
    {
        public GitTools.Logging.Logger GetLogger(string name)
        {
            return Log;
        }

        public IDisposable OpenNestedContext(string message)
        {
            throw new NotImplementedException();
        }

        public IDisposable OpenMappedContext(string key, string value)
        {
            throw new NotImplementedException();
        }

        private static bool Log(LogLevel loglevel, Func<string> messagefunc, Exception exception, object[] formatparameters)
        {
            // Create the main message. Careful of string format errors.
            string message;
            if (messagefunc == null)
            {
                message = null;
            }
            else
            {
                if (formatparameters == null || formatparameters.Length == 0)
                {
                    message = messagefunc();
                }
                else
                {
                    try
                    {
                        message = string.Format(messagefunc(), formatparameters);
                    }
                    catch (FormatException)
                    {
                        message = messagefunc();
                        Logger.WriteError(string.Format("LoggerWrapper.Log(): Incorrectly formatted string: message: '{0}'; formatparameters: {1}", message, string.Join(";", formatparameters)));
                    }
                }
            }

            if (exception != null)
            {
                // Append the exception to the end of the message.
                message = string.IsNullOrEmpty(message) ? exception.ToString() : string.Format("{0}\n{1}", message, exception);
            }

            if (!string.IsNullOrEmpty(message))
            {
                switch (loglevel)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                        Logger.WriteDebug(message);
                        break;
                    case LogLevel.Info:
                        Logger.WriteInfo(message);
                        break;
                    case LogLevel.Warn:
                        Logger.WriteWarning(message);
                        break;
                    case LogLevel.Error:
                    case LogLevel.Fatal:
                        Logger.WriteError(message);
                        break;
                }
            }

            return true;
        }
    }
}