using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

public class BaseVersion
{
    public BaseVersion(string source, bool shouldIncrement)
    {
        Source = source.NotNullOrEmpty();
        ShouldIncrement = shouldIncrement;
    }

    public BaseVersion(string source, bool shouldIncrement, SemanticVersion semanticVersion, ICommit? baseVersionSource, string? branchNameOverride)
    {
        Source = source;
        ShouldIncrement = shouldIncrement;
        SemanticVersion = semanticVersion;
        BaseVersionSource = baseVersionSource;
        BranchNameOverride = branchNameOverride;
    }

    public BaseVersion(BaseVersion baseVersion)
    {
        baseVersion.NotNull();

        Source = baseVersion.Source;
        ShouldIncrement = baseVersion.ShouldIncrement;
        SemanticVersion = baseVersion.SemanticVersion;
        BaseVersionSource = baseVersion.BaseVersionSource;
        BranchNameOverride = baseVersion.BranchNameOverride;
    }

    public string Source { get; init; }

    public bool ShouldIncrement { get; init; }

    public SemanticVersion? SemanticVersion { get; init; }

    public SemanticVersion GetSemanticVersion() => SemanticVersion ?? SemanticVersion.Empty;

    public ICommit? BaseVersionSource { get; init; }

    public string? BranchNameOverride { get; init; }

    public override string ToString()
    {
        var externalSource = BaseVersionSource == null ? "External Source" : BaseVersionSource.Sha;
        return $"{Source}: {GetSemanticVersion():f} with commit source '{externalSource}'";
    }
}
