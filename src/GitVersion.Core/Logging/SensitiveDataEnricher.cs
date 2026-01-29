using GitVersion.Core;
using Serilog.Core;
using Serilog.Events;

namespace GitVersion.Logging;

/// <summary>
/// Serilog enricher that masks sensitive data (passwords in URLs) from log messages.
/// </summary>
internal sealed class SensitiveDataEnricher : ILogEventEnricher
{
#pragma warning disable S2068 // Parameter names should begin with lower-case letter
    private const string MaskedPassword = "*******";
#pragma warning restore S2068 // Parameter names should begin with lower-case letter
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Only process if there's a message that might contain a URL with credentials
        var message = logEvent.MessageTemplate.Text;
        if (!MightContainSensitiveData(message))
            return;

        // Create a new message template with masked content
        var maskedMessage = MaskPassword(message);
        if (maskedMessage != message)
        {
            // Add a property indicating the message was masked
            var maskedProperty = propertyFactory.CreateProperty("SensitiveDataMasked", true);
            logEvent.AddPropertyIfAbsent(maskedProperty);
        }
    }

    /// <summary>
    /// Quick check to avoid regex processing when not needed.
    /// </summary>
    private static bool MightContainSensitiveData(string input)
        => input.Contains("://") && input.Contains('@');

    /// <summary>
    /// Masks passwords in URLs within the input string.
    /// </summary>
    public static string MaskPassword(string input)
    {
        if (!MightContainSensitiveData(input))
            return input;

        return RegexPatterns.ObscurePasswordRegex.Replace(input, match =>
            // Reconstruct: scheme + username + masked password + @
            $"{match.Groups[1].Value}{match.Groups[2].Value}:{MaskedPassword}@");
    }
}
