using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion;

internal class MergeCommitFinder(RepositoryStore repositoryStore, IGitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches, ILog log)
{
    private readonly ILog log = log.NotNull();
    private readonly IEnumerable<IBranch> branches = repositoryStore.ExcludingBranches(excludedBranches.NotNull());
    private readonly RepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly IGitVersionConfiguration configuration = configuration.NotNull();
    private readonly Dictionary<IBranch, List<BranchCommit>> mergeBaseCommitsCache = [];

    public IEnumerable<BranchCommit> FindMergeCommitsFor(IBranch branch)
    {
        branch = branch.NotNull();

        if (this.mergeBaseCommitsCache.TryGetValue(branch, out var mergeCommitsFor))
        {
            this.log.Debug($"Cache hit for getting merge commits for branch {branch.Name.Canonical}.");
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
                this.log.Warning($"{sourceBranch} has no tip.");
                continue;
            }

            var findMergeBase = this.repositoryStore.FindMergeBase(branch, sourceBranch);
            if (findMergeBase != null)
                yield return new(findMergeBase, sourceBranch);
        }
    }
}
