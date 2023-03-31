using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from the name of the branch.
/// BaseVersionSource is the commit where the branch was branched from its parent.
/// Does not increment.
/// </summary>
internal class VersionInBranchNameVersionStrategy : VersionStrategyBase
{
    private readonly IRepositoryStore repositoryStore;

    public VersionInBranchNameVersionStrategy(IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
        : base(versionContext) => this.repositoryStore = repositoryStore.NotNull();

    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        if (!configuration.Value.IsReleaseBranch) yield break;

        foreach (var branch in new[] { Context.CurrentBranch, configuration.Branch })
        {
            if (branch.Name.TryGetSemanticVersion(out var result, configuration.Value.VersionInBranchRegex,
                configuration.Value.LabelPrefix, configuration.Value.SemanticVersionFormat))
            {
                var commitBranchWasBranchedFrom = this.repositoryStore.FindCommitBranchWasBranchedFrom(
                    configuration.Branch, Context.Configuration
                );

                string? branchNameOverride = null;
                if (Context.CurrentBranch.Name.Equals(branch.Name)
                    || Context.Configuration.GetBranchConfiguration(Context.CurrentBranch.Name).Label is null)
                {
                    branchNameOverride = result.Name;
                }

                yield return new BaseVersion(
                    "Version in branch name", false, result.Value, commitBranchWasBranchedFrom.Commit, branchNameOverride
                );
                yield break;
            }
        }

    }
}
