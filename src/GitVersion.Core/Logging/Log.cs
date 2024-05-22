using System.Globalization;
using System.Text.RegularExpressions;
using GitVersion.Helpers;

namespace GitVersion.Logging;

internal sealed class Log(params ILogAppender[] appenders) : ILog
{
    private IEnumerable<ILogAppender> appenders = appenders;
    private readonly Regex obscurePasswordRegex = new("(https?://)(.+)(:.+@)", RegexOptions.Compiled);
    private readonly StringBuilder sb = new();
    private string currentIndentation = string.Empty;
    private const string Indentation = "  ";

    public Log() : this([])
    {
    }

    public Verbosity Verbosity { get; set; } = Verbosity.Normal;

    public void Write(Verbosity verbosity, LogLevel level, string format, params object[] args)
    {
        if (verbosity > Verbosity)
        {
            return;
        }

        var message = args.Length != 0 ? string.Format(format, args) : format;
        var formattedString = FormatMessage(message, level.ToString().ToUpperInvariant());
        foreach (var appender in this.appenders)
        {
            appender.WriteTo(level, formattedString);
        }

        this.sb.AppendLine(formattedString);
    }

    public IDisposable IndentLog(string operationDescription)
    {
        var start = TimeProvider.System.GetTimestamp();
        Write(Verbosity.Normal, LogLevel.Info, $"-< Begin: {operationDescription} >-");
        this.currentIndentation += Indentation;

        return Disposable.Create(() =>
        {
            var length = this.currentIndentation.Length - Indentation.Length;
            this.currentIndentation = length > 0 ? this.currentIndentation[..length] : "";
            var end = TimeProvider.System.GetTimestamp();
            var duration = TimeProvider.System.GetElapsedTime(start, end).TotalMilliseconds;
            Write(Verbosity.Normal, LogLevel.Info, string.Format(CultureInfo.InvariantCulture, "-< End: {0} (Took: {1:N}ms) >-", operationDescription, duration));
        });
    }

    public void Separator() => Write(Verbosity.Normal, LogLevel.Info, "-------------------------------------------------------");

    public void AddLogAppender(ILogAppender logAppender) => this.appenders = this.appenders.Concat([logAppender]);

    public override string ToString() => this.sb.ToString();

    private string FormatMessage(string message, string level)
    {
        var obscuredMessage = this.obscurePasswordRegex.Replace(message, "$1$2:*******@");
        var timestamp = $"{DateTime.Now:yy-MM-dd H:mm:ss:ff}";
        return string.Format(CultureInfo.InvariantCulture, "{0}{1} [{2}] {3}", this.currentIndentation, level, timestamp, obscuredMessage);
    }
}
