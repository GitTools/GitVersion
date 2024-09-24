using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.Configuration;

public static class ReferenceNameExtensions
{
    public static bool TryGetSemanticVersion(this ReferenceName referenceName, out (SemanticVersion Value, string? Name) result,
                                             Regex versionPatternRegex,
                                             string? tagPrefix,
                                             SemanticVersionFormat format)
    {
        result = default;

        int length = 0;
        foreach (var branchPart in referenceName.WithoutOrigin.Split(GetBranchSeparator()))
        {
            if (string.IsNullOrEmpty(branchPart)) return false;

            var match = versionPatternRegex.NotNull().Match(branchPart);
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
    }

    public static bool TryGetSemanticVersion(
        this ReferenceName source, out (SemanticVersion Value, string? Name) result, EffectiveConfiguration configuration)
        => source.TryGetSemanticVersion(out result, configuration.VersionInBranchRegex, configuration.TagPrefix, configuration.SemanticVersionFormat);
}
