using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using GitVersion.Helpers;

namespace GitVersion.Log
{
    public sealed class Log : ILog
    {
        private IDictionary<LogLevel, (ConsoleColor, ConsoleColor)> _palettes;
        private readonly object _lock;
        private static readonly Regex ObscurePasswordRegex = new Regex("(https?://)(.+)(:.+@)", RegexOptions.Compiled);
        private string indent = string.Empty;

        public Log()
        {
            _palettes = CreatePalette();
            _lock = new object();
        }
        public void Write(LogLevel level, string format, params object[] args)
        {
            lock (_lock)
            {
                try
                {
                    var (backgroundColor, foregroundColor) = _palettes[level];

                    Console.BackgroundColor = backgroundColor;
                    Console.ForegroundColor = foregroundColor;

                    var formattedString = FormatMessage(string.Format(format, args), level.ToString().ToUpperInvariant());

                    if (level == LogLevel.Error)
                    {
                        Console.Error.Write(formattedString);
                    }
                    else if (level != LogLevel.None)
                    {
                        Console.Write(formattedString);
                    }
                }
                finally
                {
                    Console.ResetColor();
                    if (level == LogLevel.Error)
                    {
                        Console.Error.WriteLine();
                    }
                    else if (level != LogLevel.None)
                    {
                        Console.WriteLine();
                    }
                }
            }
        }

        public IDisposable IndentLog(string operationDescription)
        {
            var start = DateTime.Now;
            Write(LogLevel.Info, $"Begin: {operationDescription}");
            indent += "  ";

            return Disposable.Create(() =>
            {
                var length = indent.Length - 2;
                indent = length > 0 ? indent.Substring(0, length) : indent;
                Write(LogLevel.Info, string.Format(CultureInfo.InvariantCulture, "End: {0} (Took: {1:N}ms)", operationDescription, DateTime.Now.Subtract(start).TotalMilliseconds));
            });
        }

        private string FormatMessage(string message, string level)
        {
            var obscuredMessage = ObscurePasswordRegex.Replace(message, "$1$2:*******@");
            return string.Format(CultureInfo.InvariantCulture, "{0}{1} [{2:MM/dd/yy H:mm:ss:ff}] {3}", indent, level, DateTime.Now, obscuredMessage);
        }

        private IDictionary<LogLevel, (ConsoleColor backgroundColor, ConsoleColor foregroundColor)> CreatePalette()
        {
            var background = Console.BackgroundColor;
            var palette = new Dictionary<LogLevel, (ConsoleColor, ConsoleColor)>
            {
                { LogLevel.Error, (ConsoleColor.DarkRed, ConsoleColor.White) },
                { LogLevel.Warn, (background, ConsoleColor.Yellow) },
                { LogLevel.Info, (background, ConsoleColor.White) },
                { LogLevel.Debug, (background, ConsoleColor.DarkGray) }
            };
            return palette;
        }
    }
}
