using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using GitVersion.Extensions;

namespace GitVersion;

public static class ReferenceNameExtensions
{
    public static bool TryGetSemanticVersion(this ReferenceName source,
        [NotNullWhen(true)] out (SemanticVersion Value, string? Name) result,
        Regex versionPatternRegex, string? labelPrefix, SemanticVersionFormat format)
    {
        source.NotNull();

        result = default;

        Contract.Assume(versionPatternRegex.ToString().StartsWith("^"));

        int length = 0;
        foreach (var branchPart in source.WithoutOrigin.Split(GetBranchSeparator(source)))
        {
            if (branchPart.IsNullOrEmpty()) return false;

            var match = versionPatternRegex.NotNull().Match(branchPart);
            if (match.Success)
            {
                var versionPart = match.Groups["version"].Value;
                if (SemanticVersion.TryParse(versionPart, labelPrefix, out var semanticVersion, format))
                {
                    length += versionPart.Length;
                    var name = source.WithoutOrigin[length..].Trim('-');
                    result = new(semanticVersion, name.IsEmpty() ? null : name);
                    return true;
                }
            }
            length += branchPart.Length + 1;
        }

        return false;
    }

    private static char GetBranchSeparator(ReferenceName source)
        => source.WithoutOrigin.Contains('/') || !source.WithoutOrigin.Contains('-') ? '/' : '-';
}
