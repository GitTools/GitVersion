using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GitVersion.Extensions;

public static class StringExtensions
{
    private static readonly string[] Trues = new[] { "1", "true" };
    private static readonly string[] Falses = new[] { "0", "false" };

    public static bool IsTrue(this string value) => Trues.Contains(value, StringComparer.OrdinalIgnoreCase);

    public static bool IsFalse(this string value) => Falses.Contains(value, StringComparer.OrdinalIgnoreCase);

    public static bool IsValidPath(this string path)
    {
        if (path == null)
            return false;

        try
        {
            _ = Path.GetFullPath(path);
        }
        catch
        {
            path = Path.Combine(System.Environment.CurrentDirectory, path);

            try
            {
                _ = Path.GetFullPath(path);
            }
            catch
            {
                return false;
            }
        }

        return Directory.Exists(path);
    }

    public static bool IsSwitchArgument(this string value) => value != null
                                                              && (value.StartsWith("-") || value.StartsWith("/"))
                                                              && !Regex.Match(value, @"/\w+:").Success; //Exclude msbuild & project parameters in form /blah:, which should be parsed as values, not switch names.

    public static bool IsSwitch(this string value, string switchName)
    {
        if (value == null)
            return false;

        if (value.StartsWith("-"))
        {
            value = value.Substring(1);
        }

        if (value.StartsWith("/"))
        {
            value = value.Substring(1);
        }

        return string.Equals(switchName, value, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsInit(this string singleArgument) => singleArgument.Equals("init", StringComparison.OrdinalIgnoreCase);

    public static bool IsHelp(this string singleArgument) => (singleArgument == "?") || singleArgument.IsSwitch("h") || singleArgument.IsSwitch("help") || singleArgument.IsSwitch("?");

    public static bool ArgumentRequiresValue(this string argument, int argumentIndex)
    {
        var booleanArguments = new[]
        {
            "init",
            "updateassemblyinfo",
            "ensureassemblyinfo",
            "nofetch",
            "nonormalize",
            "nocache",
        };

        var argumentMightRequireValue = !booleanArguments.Contains(argument.Substring(1), StringComparer.OrdinalIgnoreCase);

        // If this is the first argument that might be a target path, the argument starts with slash and we're on an OS that supports paths with slashes, the argument does not require a value.
        if (argumentMightRequireValue && argumentIndex == 0 && argument.StartsWith("/") && Path.DirectorySeparatorChar == '/' && argument.IsValidPath())
            return false;

        return argumentMightRequireValue;
    }

    public static void AppendLineFormat(this StringBuilder stringBuilder, string format, params object[] args)
    {
        stringBuilder.AppendFormat(format, args);
        stringBuilder.AppendLine();
    }

    public static string RegexReplace(this string input, string pattern, string replace, RegexOptions options = RegexOptions.None) => Regex.Replace(input, pattern, replace, options);

    public static bool IsEquivalentTo(this string self, string? other) =>
        string.Equals(self, other, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc cref="string.IsNullOrEmpty"/>
    public static bool IsNullOrEmpty([NotNullWhen(returnValue: false)] this string? value) => string.IsNullOrEmpty(value);

    /// <inheritdoc cref="string.IsNullOrWhiteSpace"/>
    public static bool IsNullOrWhiteSpace([NotNullWhen(returnValue: false)] this string? value) => string.IsNullOrWhiteSpace(value);
}
