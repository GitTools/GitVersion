namespace GitVersion.VersionCalculation;

internal static class VersionBumpMessageParser
{
    internal const string IncrementGroupName = "increment";

    public static VersionField? GetIncrementOverride(string message, string? pattern)
    {
        var regex = pattern is null
            ? RegexPatterns.VersionCalculation.DefaultOverrideRegex
            : RegexPatterns.Cache.GetOrAdd(pattern);
        var match = regex.Match(message);

        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups[IncrementGroupName].Value;
        if (Enum.TryParse<VersionField>(value, ignoreCase: true, out var increment)
            && Enum.IsDefined(increment)
            && value.Equals(increment.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return increment;
        }

        throw new GitVersionException(
            $"The 'override-version-bump-message' regular expression must capture one of " +
            $"'{nameof(VersionField.None)}', '{nameof(VersionField.Patch)}', " +
            $"'{nameof(VersionField.Minor)}', or '{nameof(VersionField.Major)}' " +
            $"in a named group called '{IncrementGroupName}'."
        );
    }
}
