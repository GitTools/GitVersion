using GitVersion.Common;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion;

internal class BranchesContainingCommitFinder(IRepositoryStore repositoryStore, ILog log)
{
    private readonly ILog log = log.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();

    public IEnumerable<IBranch> GetBranchesContainingCommit(ICommit commit, IEnumerable<IBranch>? branches = null, bool onlyTrackedBranches = false)
    {
        commit.NotNull();
        branches ??= [.. this.repositoryStore.Branches];

        // TODO Should we cache this?
        // Yielding part is split from the main part of the method to avoid having the exception check performed lazily.
        // Details at https://github.com/GitTools/GitVersion/issues/2755
        return InnerGetBranchesContainingCommit(commit, branches, onlyTrackedBranches);
    }

    private IEnumerable<IBranch> InnerGetBranchesContainingCommit(IGitObject commit, IEnumerable<IBranch> branches, bool onlyTrackedBranches)
    {
        using (log.IndentLog($"Getting branches containing the commit '{commit.Id}'."))
        {
            var directBranchHasBeenFound = false;
            log.Info("Trying to find direct branches.");
            // TODO: It looks wasteful looping through the branches twice. Can't these loops be merged somehow? @asbjornu
            var branchList = branches.ToList();
            foreach (var branch in branchList.Where(branch => BranchTipIsNullOrCommit(branch, commit) && !IncludeTrackedBranches(branch, onlyTrackedBranches)))
            {
                directBranchHasBeenFound = true;
                log.Info($"Direct branch found: '{branch}'.");
                yield return branch;
            }

            if (directBranchHasBeenFound)
            {
                yield break;
            }

            log.Info($"No direct branches found, searching through {(onlyTrackedBranches ? "tracked" : "all")} branches.");
            foreach (var branch in branchList.Where(b => IncludeTrackedBranches(b, onlyTrackedBranches)))
            {
                log.Info($"Searching for commits reachable from '{branch}'.");

                var commits = this.repositoryStore.GetCommitsReacheableFrom(commit, branch);

                if (!commits.Any())
                {
                    log.Info($"The branch '{branch}' has no matching commits.");
                    continue;
                }

                log.Info($"The branch '{branch}' has a matching commit.");
                yield return branch;
            }
        }
    }

    private static bool IncludeTrackedBranches(IBranch branch, bool includeOnlyTracked)
        => (includeOnlyTracked && branch.IsTracking) || !includeOnlyTracked;

    private static bool BranchTipIsNullOrCommit(IBranch branch, IGitObject commit)
        => branch.Tip == null || branch.Tip.Sha == commit.Sha;
}
