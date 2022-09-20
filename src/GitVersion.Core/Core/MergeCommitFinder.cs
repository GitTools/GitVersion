using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion;

internal class MergeCommitFinder
{
    private readonly IEnumerable<IBranch> excludedBranches;
    private readonly ILog log;
    private readonly Dictionary<IBranch?, List<BranchCommit>> mergeBaseCommitsCache = new();
    private readonly RepositoryStore repositoryStore;
    private readonly Config configuration;

    public MergeCommitFinder(RepositoryStore repositoryStore, Config configuration, IEnumerable<IBranch> excludedBranches, ILog log)
    {
        this.repositoryStore = repositoryStore.NotNull();
        this.configuration = configuration.NotNull();
        this.excludedBranches = repositoryStore.ExcludingBranches(excludedBranches.NotNull(), excludeRemotes: false);
        this.log = log.NotNull();
    }

    public IEnumerable<BranchCommit> FindMergeCommitsFor(IBranch branch)
    {
        branch = branch.NotNull();

        if (this.mergeBaseCommitsCache.ContainsKey(branch))
        {
            this.log.Debug($"Cache hit for getting merge commits for branch {branch?.Name.Canonical}.");
            return this.mergeBaseCommitsCache[branch];
        }

        var branchMergeBases = FindMergeBases(branch)
            .OrderByDescending(b => b.Commit.When)
            .ToList();

        this.mergeBaseCommitsCache.Add(branch, branchMergeBases);

        return branchMergeBases.Where(b => !branch.Name.EquivalentTo(b.Branch.Name.WithoutRemote));
    }

    private IEnumerable<BranchCommit> FindMergeBases(IBranch branch)
    {
        var sourceBranches = new SourceBranchFinder(this.excludedBranches, this.configuration)
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
