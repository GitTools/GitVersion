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
internal sealed class VersionInBranchNameVersionStrategy(
    Lazy<GitVersionContext> contextLazy,
    IRepositoryStore repositoryStore)
    : IVersionStrategy
{
    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();

    private GitVersionContext Context => contextLazy.Value;

    public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        if (!Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.VersionInBranchName))
            yield break;

        if (TryGetBaseVersion(configuration, out var baseVersion))
        {
            yield return baseVersion;
        }
    }

    public bool TryGetBaseVersion(EffectiveBranchConfiguration configuration, [NotNullWhen(true)] out BaseVersion? baseVersion)
    {
        baseVersion = null;

        if (!configuration.Value.IsReleaseBranch)
            return false;

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

                var label = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, branchNameOverride);
                //if (configuration.Value.Label != label)
                //{
                //    log.Info("Using current branch name to calculate version tag");
                //}

                baseVersion = new BaseVersion("Version in branch name", result.Value, commitBranchWasBranchedFrom.Value.Commit)
                {
                    Operator = new BaseVersionOperator()
                    {
                        Increment = VersionField.None,
                        ForceIncrement = false,
                        Label = label
                    }
                };
                return true;
            }
        }

        return false;
    }
}
