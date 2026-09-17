using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Caching;

internal sealed class CacheRepositoryTargetProvider(
    IOptions<GitVersionOptions> options,
    BranchResolver branchResolver,
    GitPreparer gitPreparer)
{
    public string[] GetTarget()
    {
        var commitId = options.Value.RepositoryInfo.CommitId;
        if (branchResolver.ContextBranch is { } context)
        {
            // A fetched remote can change contextual PR target selection even
            // when its refs already matched the remote before preparation.
            return ["branch-context", context.Canonical, commitId ?? string.Empty, gitPreparer.FetchedRemoteName ?? string.Empty];
        }

        return branchResolver.TargetBranch.IsNullOrEmpty() && commitId.IsNullOrEmpty()
            ? []
            : [branchResolver.TargetBranch ?? string.Empty, commitId ?? string.Empty];
    }
}
