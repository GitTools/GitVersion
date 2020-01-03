using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitVersion.Logging
{
    public sealed class Log : ILog
    {
        private IEnumerable<ILogAppender> appenders;
        private readonly Regex obscurePasswordRegex = new Regex("(https?://)(.+)(:.+@)", RegexOptions.Compiled);
        private readonly StringBuilder sb;
        private string indent = string.Empty;

        public Log(): this(Array.Empty<ILogAppender>())
        {
        }

        public Log(params ILogAppender[] appenders)
        {
            this.appenders = appenders ?? Array.Empty<ILogAppender>();
            sb = new StringBuilder();
            Verbosity = Verbosity.Normal;
        }

        public Verbosity Verbosity { get; set; }

        public void Write(Verbosity verbosity, LogLevel level, string format, params object[] args)
        {
            if (verbosity > Verbosity)
            {
                return;
            }

            var message = args.Any() ? string.Format(format, args) : format;
            var formattedString = FormatMessage(message, level.ToString().ToUpperInvariant());
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

        public void AddLogAppender(ILogAppender logAppender)
        {
            appenders = appenders.Concat(new[] { logAppender });
        }

        public override string ToString()
        {
            return sb.ToString();
        }

        private string FormatMessage(string message, string level)
        {
            var obscuredMessage = obscurePasswordRegex.Replace(message, "$1$2:*******@");
            return string.Format(CultureInfo.InvariantCulture, "{0}{1} [{2:MM/dd/yy H:mm:ss:ff}] {3}", indent, level, DateTime.Now, obscuredMessage);
        }
    }
}
