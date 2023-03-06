using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from the name of the branch.
/// BaseVersionSource is the commit where the branch was branched from its parent.
/// Does not increment.
/// </summary>
public class VersionInBranchNameVersionStrategy : VersionStrategyBase
{
    private readonly IRepositoryStore repositoryStore;

    public VersionInBranchNameVersionStrategy(IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
        : base(versionContext) => this.repositoryStore = repositoryStore.NotNull();

    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        if (!configuration.Value.IsReleaseBranch) yield break;

        var versionInBranch = GetVersionInBranch(
            configuration.Branch.Name, configuration.Value.LabelPrefix, configuration.Value.SemanticVersionFormat
        );
        if (versionInBranch != null)
        {
            var commitBranchWasBranchedFrom = this.repositoryStore.FindCommitBranchWasBranchedFrom(
                configuration.Branch, Context.Configuration
            );
            var branchNameOverride = Context.CurrentBranch.Name.Friendly.RegexReplace("[-/]" + versionInBranch.Item1, string.Empty);
            yield return new BaseVersion("Version in branch name", false, versionInBranch.Item2, commitBranchWasBranchedFrom.Commit, branchNameOverride);
        }
    }

    private static Tuple<string, SemanticVersion>? GetVersionInBranch(
        ReferenceName branchName, string? tagPrefixRegex, SemanticVersionFormat versionFormat)
    {
        var branchParts = branchName.WithoutOrigin.Split('/', '-');
        foreach (var part in branchParts)
        {
            if (SemanticVersion.TryParse(part, tagPrefixRegex, out var semanticVersion, versionFormat))
            {
                return Tuple.Create(part, semanticVersion);
            }
        }

        return null;
    }
}
