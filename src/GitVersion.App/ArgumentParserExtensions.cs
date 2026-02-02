using GitVersion.Core;
using GitVersion.Helpers;

namespace GitVersion;

#pragma warning disable S1144
internal static class ArgumentParserExtensions
{
    private static readonly string[] TrueValues = ["1", "true"];
    private static readonly string[] FalseValues = ["0", "false"];

    extension(string? value)
    {
        public bool IsTrue() => TrueValues.Contains(value, StringComparer.OrdinalIgnoreCase);
        public bool IsFalse() => FalseValues.Contains(value, StringComparer.OrdinalIgnoreCase);

        public bool IsValidPath()
        {
            if (value == null)
                return false;

            try
            {
                _ = FileSystemHelper.Path.GetFullPath(value);
            }
            catch
            {
                value = FileSystemHelper.Path.Combine(SysEnv.CurrentDirectory, value);

                try
                {
                    _ = FileSystemHelper.Path.GetFullPath(value);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return FileSystemHelper.Directory.Exists(value);
        }

        public bool IsSwitchArgument()
        {
            var patternRegex = RegexPatterns.SwitchArgumentRegex;
            return value != null
                   && (value.StartsWith('-') || value.StartsWith('/'))
                   && !patternRegex.Match(value).Success;
            //Exclude msbuild & project parameters in form /blah:, which should be parsed as values, not switch names.
        }

        public bool IsSwitch(string switchName)
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
    }

    extension(string singleArgument)
    {
        public bool IsHelp() => (singleArgument == "?") || singleArgument.IsSwitch("h") || singleArgument.IsSwitch("help") || singleArgument.IsSwitch("?");

        public bool ArgumentRequiresValue(int argumentIndex)
        {
            var booleanArguments = new[] { "updateassemblyinfo", "ensureassemblyinfo", "nofetch", "nonormalize", "nocache", "allowshallow", "diag" };

            var argumentMightRequireValue = !booleanArguments.Contains(singleArgument[1..], StringComparer.OrdinalIgnoreCase);

            // If this is the first argument that might be a target path, the argument starts with slash, and we're on an OS that supports paths with slashes, the argument does not require a value.
            if (argumentMightRequireValue && argumentIndex == 0 && singleArgument.StartsWith('/') && FileSystemHelper.Path.DirectorySeparatorChar == '/' && singleArgument.IsValidPath())
                return false;

            return argumentMightRequireValue;
        }
    }
}
#pragma warning restore S1144
