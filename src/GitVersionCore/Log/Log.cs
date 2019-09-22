using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GitVersion.Log
{
    public sealed class Log : ILog
    {
        private readonly IEnumerable<ILogAppender> appenders;
        private static readonly Regex ObscurePasswordRegex = new Regex("(https?://)(.+)(:.+@)", RegexOptions.Compiled);
        private string indent = string.Empty;
        private StringBuilder sb;

        public Log(params ILogAppender[] appenders)
        {
            this.appenders = appenders ?? Array.Empty<ILogAppender>();
            sb = new StringBuilder();
        }

        public Verbosity Verbosity { get; set; }

        public void Write(Verbosity verbosity, LogLevel level, string format, params object[] args)
        {
            if (verbosity > Verbosity)
            {
                return;
            }

            var formattedString = FormatMessage(string.Format(format, args), level.ToString().ToUpperInvariant());
            foreach (var appender in appenders)
            {
                appender.WriteTo(level, formattedString);
            }

            sb.Append(formattedString);
        }

        public IDisposable IndentLog(string operationDescription)
        {
            var start = DateTime.Now;
            Write(Verbosity.Normal, LogLevel.Info, $"Begin: {operationDescription}");
            indent += "  ";

            return Disposable.Create(() =>
            {
                var length = indent.Length - 2;
                indent = length > 0 ? indent.Substring(0, length) : indent;
                Write(Verbosity.Normal, LogLevel.Info, string.Format(CultureInfo.InvariantCulture, "End: {0} (Took: {1:N}ms)", operationDescription, DateTime.Now.Subtract(start).TotalMilliseconds));
            });
        }

        public override string ToString()
        {
            return sb.ToString();
        }

        private string FormatMessage(string message, string level)
        {
            var obscuredMessage = ObscurePasswordRegex.Replace(message, "$1$2:*******@");
            return string.Format(CultureInfo.InvariantCulture, "{0}{1} [{2:MM/dd/yy H:mm:ss:ff}] {3}", indent, level, DateTime.Now, obscuredMessage);
        }
    }
}
