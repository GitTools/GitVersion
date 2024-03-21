using System.Text.RegularExpressions;
using GitVersion.Helpers;

namespace GitVersion;

internal static class ArgumentParserExtensions
{
    private static readonly string[] TrueValues = ["1", "true"];
    private static readonly string[] FalseValues = ["0", "false"];

    public static bool IsTrue(this string? value) => TrueValues.Contains(value, StringComparer.OrdinalIgnoreCase);

    public static bool IsFalse(this string? value) => FalseValues.Contains(value, StringComparer.OrdinalIgnoreCase);

    public static bool IsValidPath(this string? path)
    {
        if (path == null)
            return false;

        try
        {
            _ = PathHelper.GetFullPath(path);
        }
        catch
        {
            path = PathHelper.Combine(SysEnv.CurrentDirectory, path);

            try
            {
                _ = PathHelper.GetFullPath(path);
            }
            catch
            {
                return false;
            }
        }

        return Directory.Exists(path);
    }

    public static bool IsSwitchArgument(this string? value)
        => value != null
           && (value.StartsWith('-') || value.StartsWith('/'))
           && !Regex.Match(value, @"/\w+:").Success; //Exclude msbuild & project parameters in form /blah:, which should be parsed as values, not switch names.

    public static bool IsSwitch(this string? value, string switchName)
    {
        if (value == null)
            return false;

        if (value.StartsWith('-'))
        {
            value = value[1..];
        }

        if (value.StartsWith('/'))
        {
            value = value[1..];
        }

        return string.Equals(switchName, value, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsHelp(this string singleArgument) => (singleArgument == "?") || singleArgument.IsSwitch("h") || singleArgument.IsSwitch("help") || singleArgument.IsSwitch("?");

    public static bool ArgumentRequiresValue(this string argument, int argumentIndex)
    {
        var booleanArguments = new[] { "init", "updateassemblyinfo", "ensureassemblyinfo", "nofetch", "nonormalize", "nocache" };

        var argumentMightRequireValue = !booleanArguments.Contains(argument[1..], StringComparer.OrdinalIgnoreCase);

        // If this is the first argument that might be a target path, the argument starts with slash and we're on an OS that supports paths with slashes, the argument does not require a value.
        if (argumentMightRequireValue && argumentIndex == 0 && argument.StartsWith('/') && Path.DirectorySeparatorChar == '/' && argument.IsValidPath())
            return false;

        return argumentMightRequireValue;
    }
}
