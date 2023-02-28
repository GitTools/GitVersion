using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion;

internal class MergeCommitFinder
{
    private readonly IEnumerable<IBranch> branches;
    private readonly ILog log;
    private readonly Dictionary<IBranch, List<BranchCommit>> mergeBaseCommitsCache = new();
    private readonly RepositoryStore repositoryStore;
    private readonly GitVersionConfiguration configuration;

    public MergeCommitFinder(RepositoryStore repositoryStore, GitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches, ILog log)
    {
        this.repositoryStore = repositoryStore.NotNull();
        this.configuration = configuration.NotNull();
        this.branches = repositoryStore.ExcludingBranches(excludedBranches.NotNull());
        this.log = log.NotNull();
    }

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

        return branchMergeBases.Where(b => !branch.Name.EquivalentTo(b.Branch.Name.WithoutRemote));
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
                yield return new BranchCommit(findMergeBase, sourceBranch);
        }
    }
}
