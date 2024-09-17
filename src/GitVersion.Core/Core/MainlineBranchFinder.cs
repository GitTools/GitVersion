using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion;

internal class MainlineBranchFinder(
    IRepositoryStore repositoryStore,
    IGitRepository repository,
    IGitVersionConfiguration configuration,
    ILog log)
{
    private readonly IGitVersionConfiguration configuration = configuration.NotNull();
    private readonly ILog log = log.NotNull();
    private readonly IGitRepository repository = repository.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly List<IBranchConfiguration> mainlineBranchConfigurations =
        configuration.Branches.Select(e => e.Value).Where(b => b.IsMainBranch == true).ToList();

    public IDictionary<string, List<IBranch>> FindMainlineBranches(ICommit commit)
    {
        var branchOriginFinder = new BranchOriginFinder(commit, this.repositoryStore, this.configuration, this.log);
        return this.repository.Branches
            .Where(IsMainBranch)
            .Select(branchOriginFinder.BranchOrigin)
            .Where(bc => bc != BranchCommit.Empty)
            .GroupBy(bc => bc.Commit.Sha, bc => bc.Branch)
            .ToDictionary(group => group.Key, x => x.ToList());
    }

    private bool IsMainBranch(INamedReference branch)
    {
        var matcher = new MainlineConfigBranchMatcher(branch, this.log);
        return this.mainlineBranchConfigurations.Any(matcher.IsMainBranch);
    }

    private class MainlineConfigBranchMatcher(INamedReference branch, ILog log)
    {
        private readonly INamedReference branch = branch.NotNull();
        private readonly ILog log = log.NotNull();

        public bool IsMainBranch(IBranchConfiguration value)
        {
            if (value.RegularExpression == null)
                return false;

            var regex = RegexPatterns.Cache.GetOrAdd(value.RegularExpression);
            var branchName = this.branch.Name.WithoutOrigin;
            var match = regex.IsMatch(branchName);
            this.log.Info($"'{value.RegularExpression}' {(match ? "matches" : "does not match")} '{branchName}'.");
            return match;
        }
    }

    private class BranchOriginFinder(ICommit commit, IRepositoryStore repositoryStore, IGitVersionConfiguration configuration, ILog log)
    {
        private readonly ICommit commit = commit.NotNull();
        private readonly IGitVersionConfiguration configuration = configuration.NotNull();
        private readonly ILog log = log.NotNull();
        private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();

        public BranchCommit BranchOrigin(IBranch branch)
        {
            var branchOrigin = FindBranchOrigin(branch);
            return branchOrigin == null
                ? BranchCommit.Empty
                : new(branchOrigin, branch);
        }

        private ICommit? FindBranchOrigin(IBranch branch)
        {
            if (branch.Tip == null)
                return null;

            var branchName = branch.Name.Friendly;
            var mergeBase = this.repositoryStore.FindMergeBase(branch.Tip, this.commit);
            if (mergeBase is not null)
            {
                this.log.Info($"Found merge base {mergeBase.Sha} for '{branchName}'.");
                return mergeBase;
            }

            var branchCommit = this.repositoryStore.FindCommitBranchBranchedFrom(branch, this.configuration);
            if (branchCommit != BranchCommit.Empty)
            {
                this.log.Info($"Found parent commit {branchCommit.Commit.Sha} for '{branchName}'.");
                return branchCommit.Commit;
            }

            this.log.Info($"Found no merge base or parent commit for '{branchName}'.");
            return null;
        }
    }
}
