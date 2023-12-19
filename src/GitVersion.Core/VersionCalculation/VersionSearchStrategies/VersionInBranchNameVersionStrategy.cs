using System.Diagnostics.CodeAnalysis;
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
        if (Context.Configuration.VersioningMode == VersioningMode.TrunkBased) yield break;

        if (configuration.Value.IsReleaseBranch && TryGetBaseVersion(out var baseVersion, configuration))
        {
            yield return baseVersion;
        }
    }

    private bool TryGetBaseVersion([NotNullWhen(true)] out BaseVersion? baseVersion, EffectiveBranchConfiguration configuration)
    {
        baseVersion = null;

        Lazy<BranchCommit> commitBranchWasBranchedFrom = new(
            () => this.repositoryStore.FindCommitBranchWasBranchedFrom(configuration.Branch, Context.Configuration)
        );
        foreach (var branch in new[] { Context.CurrentBranch, configuration.Branch })
        {
            if (branch.Name.TryGetSemanticVersion(out var result, configuration.Value.VersionInBranchRegex,
                configuration.Value.TagPrefix, configuration.Value.SemanticVersionFormat))
            {
                string? branchNameOverride = null;
                if (!result.Name.IsNullOrEmpty() && (Context.CurrentBranch.Name.Equals(branch.Name)
                    || Context.Configuration.GetBranchConfiguration(Context.CurrentBranch.Name).Label is null))
                {
                    branchNameOverride = result.Name;
                }

                baseVersion = new BaseVersion(
                    "Version in branch name", false, result.Value, commitBranchWasBranchedFrom.Value.Commit, branchNameOverride
                );
                break;
            }
        }

        return baseVersion != null;
    }
}
