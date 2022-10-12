using System.Text.RegularExpressions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;

namespace GitVersion;

internal class SourceBranchFinder
{
    private readonly GitVersionConfiguration configuration;
    private readonly IEnumerable<IBranch> excludedBranches;

    public SourceBranchFinder(IEnumerable<IBranch> excludedBranches, GitVersionConfiguration configuration)
    {
        this.excludedBranches = excludedBranches.NotNull();
        this.configuration = configuration.NotNull();
    }

    public IEnumerable<IBranch> FindSourceBranchesOf(IBranch branch)
    {
        var predicate = new SourceBranchPredicate(branch, this.configuration);
        return this.excludedBranches.Where(predicate.IsSourceBranch);
    }

    private class SourceBranchPredicate
    {
        private readonly IBranch branch;
        private readonly IEnumerable<string> sourceBranchRegexes;

        public SourceBranchPredicate(IBranch branch, GitVersionConfiguration configuration)
        {
            this.branch = branch;
            this.sourceBranchRegexes = GetSourceBranchRegexes(branch, configuration);
        }

        public bool IsSourceBranch(INamedReference sourceBranchCandidate)
        {
            if (Equals(sourceBranchCandidate, this.branch))
                return false;

            var branchName = sourceBranchCandidate.Name.Friendly;

            return this.sourceBranchRegexes
                .Any(regex => Regex.IsMatch(branchName, regex));
        }

        private static IEnumerable<string> GetSourceBranchRegexes(INamedReference branch, GitVersionConfiguration configuration)
        {
            var branchName = branch.Name.WithoutRemote;
            var currentBranchConfig = configuration.GetBranchConfiguration(branchName);
            if (currentBranchConfig.SourceBranches == null)
            {
                yield return ".*";
            }
            else
            {
                foreach (var sourceBranch in currentBranchConfig.SourceBranches)
                {
                    var regex = configuration.Branches[sourceBranch]?.Regex;
                    if (regex != null)
                        yield return regex;
                }
            }
        }
    }
}
