using Serilog.Core;
using Serilog.Events;

namespace GitVersion.Logging;

/// <summary>
/// Serilog enricher that adds indentation to log messages based on the current scope depth.
/// Works with <see cref="LoggerExtensions.StartIndentedScope"/> to provide visual nesting.
/// </summary>
internal sealed class IndentationEnricher : ILogEventEnricher
{
    /// <summary>
    /// The property name used to store the indentation.
    /// </summary>
    public const string IndentPropertyName = "Indent";

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var indentation = LoggerExtensions.GetIndentation();
        var indentProperty = propertyFactory.CreateProperty(IndentPropertyName, indentation);
        logEvent.AddPropertyIfAbsent(indentProperty);
    }
}
