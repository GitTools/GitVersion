using GitVersion.Core;

namespace GitVersion.Helpers;

internal class InputSanitizer : IInputSanitizer
{
    public string SanitizeFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new FormatException("Format string cannot be empty.");
        }

        if (format.Length > 50)
        {
            throw new FormatException($"Format string too long: '{format[..20]}...'");
        }

        if (format.Any(c => char.IsControl(c) && c != '\t'))
        {
            throw new FormatException("Format string contains invalid control characters");
        }

        return format;
    }

    public string SanitizeEnvVarName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Environment variable name cannot be null or empty.");
        }

        if (name.Length > 200)
        {
            throw new ArgumentException($"Environment variable name too long: '{name[..20]}...'");
        }

        if (!RegexPatterns.Cache.GetOrAdd(RegexPatterns.Common.SanitizeEnvVarNameRegexPattern).IsMatch(name))
        {
            throw new ArgumentException($"Environment variable name contains disallowed characters: '{name}'");
        }

        return name;
    }

    public string SanitizeMemberName(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName))
        {
            throw new ArgumentException("Member name cannot be empty.");
        }

        if (memberName.Length > 100)
        {
            throw new ArgumentException($"Member name too long: '{memberName[..20]}...'");
        }

        if (!RegexPatterns.Cache.GetOrAdd(RegexPatterns.Common.SanitizeMemberNameRegexPattern).IsMatch(memberName))
        {
            throw new ArgumentException($"Member name contains disallowed characters: '{memberName}'");
        }

        return memberName;
    }
}
