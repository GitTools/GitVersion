using GitVersion.Git;

namespace GitVersion.Configuration;

public static class ReferenceNameExtensions
{
    public static bool TryGetSemanticVersion(
            this ReferenceName source, out (SemanticVersion Value, string? Name) result, IGitVersionConfiguration configuration)
        => source.TryGetSemanticVersion(out result, configuration.VersionInBranchRegex, configuration.TagPrefix, configuration.SemanticVersionFormat);

    public static bool TryGetSemanticVersion(
            this ReferenceName source, out (SemanticVersion Value, string? Name) result, EffectiveConfiguration configuration)
        => source.TryGetSemanticVersion(out result, configuration.VersionInBranchRegex, configuration.TagPrefix, configuration.SemanticVersionFormat);
}
