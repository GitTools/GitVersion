using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.Configuration;

public static class ReferenceNameExtensions
{
    public static bool TryGetSemanticVersion(this ReferenceName source, EffectiveConfiguration configuration, out (SemanticVersion Value, string? Name) result)
        => source.TryGetSemanticVersion(configuration.VersionInBranchPattern, configuration.TagPrefixPattern, configuration.SemanticVersionFormat, out result);

    public static bool TryGetSemanticVersion(this ReferenceName source, IGitVersionConfiguration configuration, out (SemanticVersion Value, string? Name) result)
        => source.TryGetSemanticVersion(configuration.VersionInBranchPattern, configuration.TagPrefixPattern, configuration.SemanticVersionFormat, out result);

    private static bool TryGetSemanticVersion(this ReferenceName referenceName,
                                             string? versionPatternPattern,
                                             string? tagPrefix,
                                             SemanticVersionFormat format, out (SemanticVersion Value, string? Name) result)
    {
        var versionPatternRegex = RegexPatterns.Cache.GetOrAdd(GetVersionInBranchPattern(versionPatternPattern));
        result = default;

        var length = 0;
        foreach (var branchPart in referenceName.WithoutOrigin.Split(GetBranchSeparator()))
        {
            if (string.IsNullOrEmpty(branchPart)) return false;

            var match = versionPatternRegex.Match(branchPart);
            if (match.Success)
            {
                var versionPart = match.Groups["version"].Value;
                if (SemanticVersion.TryParse(versionPart, tagPrefix, out var semanticVersion, format))
                {
                    length += versionPart.Length;
                    var name = referenceName.WithoutOrigin[length..].Trim('-');
                    result = new(semanticVersion, name.Length == 0 ? null : name);
                    return true;
                }
            }

            length += branchPart.Length + 1;
        }

        return false;

        char GetBranchSeparator() => referenceName.WithoutOrigin.Contains('/') || !referenceName.WithoutOrigin.Contains('-') ? '/' : '-';

        static string GetVersionInBranchPattern(string? versionInBranchPattern)
        {
            if (versionInBranchPattern.IsNullOrEmpty()) versionInBranchPattern = RegexPatterns.Configuration.DefaultVersionInBranchRegexPattern;
            return $"^{versionInBranchPattern.TrimStart('^')}";
        }
    }
}
