using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Git;
using SemanticVersionResult = (GitVersion.SemanticVersion Value, string? Name);

namespace GitVersion.Configuration;

public static class ReferenceNameExtensions
{
    extension(ReferenceName source)
    {
        public bool TryGetSemanticVersion(EffectiveConfiguration configuration, out SemanticVersionResult result)
            => source.TryGetSemanticVersion(configuration.VersionInBranchPattern, configuration.TagPrefixPattern, configuration.SemanticVersionFormat, out result);

        public bool TryGetSemanticVersion(IGitVersionConfiguration configuration, out SemanticVersionResult result)
            => source.TryGetSemanticVersion(configuration.VersionInBranchPattern, configuration.TagPrefixPattern, configuration.SemanticVersionFormat, out result);

        private bool TryGetSemanticVersion(string? versionPatternPattern,
                                           string? tagPrefix,
                                           SemanticVersionFormat format,
                                           // ReSharper disable once OutParameterValueIsAlwaysDiscarded.Local
                                           out SemanticVersionResult result)
        {
            var versionPatternRegex = RegexPatterns.Cache.GetOrAdd(GetVersionInBranchPattern(versionPatternPattern));
            result = default;

            var length = 0;
            foreach (var branchPart in source.WithoutOrigin.Split(GetBranchSeparator()))
            {
                if (string.IsNullOrEmpty(branchPart)) return false;

                var match = versionPatternRegex.Match(branchPart);
                if (match.Success)
                {
                    var versionPart = match.Groups["version"].Value;
                    if (SemanticVersion.TryParse(versionPart, tagPrefix, out var semanticVersion, format))
                    {
                        length += versionPart.Length;
                        var name = source.WithoutOrigin[length..].Trim('-');
                        result = new(semanticVersion, name.Length == 0 ? null : name);
                        return true;
                    }
                }

                length += branchPart.Length + 1;
            }

            return false;

            char GetBranchSeparator() => source.WithoutOrigin.Contains('/') || !source.WithoutOrigin.Contains('-') ? '/' : '-';

            static string GetVersionInBranchPattern(string? versionInBranchPattern)
            {
                if (versionInBranchPattern.IsNullOrEmpty()) versionInBranchPattern = RegexPatterns.Configuration.DefaultVersionInBranchRegexPattern;
                return $"^{versionInBranchPattern.TrimStart('^')}";
            }
        }
    }
}
