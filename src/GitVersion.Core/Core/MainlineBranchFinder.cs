using System.Text.RegularExpressions;
using GitVersion.Common;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configurations;

namespace GitVersion;

internal class MainlineBranchFinder
{
    private readonly Configuration configuration;
    private readonly ILog log;
    private readonly List<BranchConfiguration> mainlineBranchConfigurations;
    private readonly IGitRepository repository;
    private readonly IRepositoryStore repositoryStore;

    public MainlineBranchFinder(IRepositoryStore repositoryStore,
                                IGitRepository repository,
                                Model.Configurations.Configuration configuration,
                                ILog log)
    {
        this.repositoryStore = repositoryStore.NotNull();
        this.repository = repository.NotNull();
        this.configuration = configuration.NotNull();
        mainlineBranchConfigurations = configuration.Branches.Select(e => e.Value).Where(b => b?.IsMainline == true).ToList();
        this.log = log.NotNull();
    }


    public IDictionary<string, List<IBranch>> FindMainlineBranches(ICommit commit)
    {
        var branchOriginFinder = new BranchOriginFinder(commit, this.repositoryStore, this.configuration, this.log);
        return this.repository.Branches
            .Where(BranchIsMainline)
            .Select(branchOriginFinder.BranchOrigin)
            .Where(bc => bc != BranchCommit.Empty)
            .GroupBy(bc => bc.Commit.Sha, bc => bc.Branch)
            .ToDictionary(group => group.Key, x => x.ToList());
    }


    private bool BranchIsMainline(INamedReference branch)
    {
        var matcher = new MainlineConfigBranchMatcher(branch, this.log);
        return this.mainlineBranchConfigurations.Any(matcher.IsMainline) == true;
    }

    private class MainlineConfigBranchMatcher
    {
        private readonly INamedReference branch;
        private readonly ILog log;

        public MainlineConfigBranchMatcher(INamedReference branch, ILog log)
        {
            this.branch = branch;
            this.log = log;
        }

        public bool IsMainline(BranchConfiguration value)
        {
            if (value?.Regex == null)
                return false;

            var mainlineRegex = value.Regex;
            var branchName = this.branch.Name.WithoutRemote;
            var match = Regex.IsMatch(branchName, mainlineRegex);
            this.log.Info($"'{mainlineRegex}' {(match ? "matches" : "does not match")} '{branchName}'.");
            return match;
        }
    }


    private class BranchOriginFinder
    {
        private readonly ICommit commit;
        private readonly Model.Configurations.Configuration configuration;
        private readonly ILog log;
        private readonly IRepositoryStore repositoryStore;

        public BranchOriginFinder(ICommit commit, IRepositoryStore repositoryStore, Model.Configurations.Configuration configuration, ILog log)
        {
            this.repositoryStore = repositoryStore;
            this.commit = commit;
            this.configuration = configuration;
            this.log = log;
        }

        public BranchCommit BranchOrigin(IBranch branch)
        {
            var branchOrigin = FindBranchOrigin(branch);
            return branchOrigin == null
                ? BranchCommit.Empty
                : new BranchCommit(branchOrigin, branch);
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

            var branchCommit = this.repositoryStore.FindCommitBranchWasBranchedFrom(branch, this.configuration);
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
