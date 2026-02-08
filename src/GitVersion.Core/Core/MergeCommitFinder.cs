using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion;

internal class MergeCommitFinder(
    IRepositoryStore repositoryStore,
    IGitVersionConfiguration configuration,
    IEnumerable<IBranch> excludedBranches,
    ILogger logger)
{
    private readonly IEnumerable<IBranch> branches = repositoryStore.ExcludingBranches(excludedBranches.NotNull());
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly IGitVersionConfiguration configuration = configuration.NotNull();
    private readonly ILogger logger = logger.NotNull();
    private readonly Dictionary<IBranch, List<BranchCommit>> mergeBaseCommitsCache = [];

    public IEnumerable<BranchCommit> FindMergeCommitsFor(IBranch branch)
    {
        branch = branch.NotNull();

        if (this.mergeBaseCommitsCache.TryGetValue(branch, out var mergeCommitsFor))
        {
            this.logger.LogDebug("Cache hit for getting merge commits for branch {BranchName}.", branch.Name.Canonical);
            return mergeCommitsFor;
        }

        var branchMergeBases = FindMergeBases(branch)
            .OrderByDescending(b => b.Commit.When)
            .ToList();

        this.mergeBaseCommitsCache.Add(branch, branchMergeBases);

        return branchMergeBases.Where(b => !branch.Name.EquivalentTo(b.Branch.Name.WithoutOrigin));
    }

    private IEnumerable<BranchCommit> FindMergeBases(IBranch branch)
    {
        var sourceBranches = new SourceBranchFinder(this.branches, this.configuration)
            .FindSourceBranchesOf(branch);

        foreach (var sourceBranch in sourceBranches)
        {
            if (sourceBranch.Tip == null)
            {
                this.logger.LogWarning("Branch {SourceBranch} has no tip.", sourceBranch);
                continue;
            }

            var findMergeBase = this.repositoryStore.FindMergeBase(branch, sourceBranch);
            if (findMergeBase != null)
                yield return new(findMergeBase, sourceBranch);
        }
    }
}
