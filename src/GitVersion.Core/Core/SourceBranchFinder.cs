using System.Text.RegularExpressions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion;

internal class SourceBranchFinder(IEnumerable<IBranch> excludedBranches, IGitVersionConfiguration configuration)
{
    private readonly IGitVersionConfiguration configuration = configuration.NotNull();
    private readonly IEnumerable<IBranch> excludedBranches = excludedBranches.NotNull();

    public IEnumerable<IBranch> FindSourceBranchesOf(IBranch branch)
    {
        var predicate = new SourceBranchPredicate(branch, this.configuration);
        return this.excludedBranches.Where(predicate.IsSourceBranch);
    }

    private class SourceBranchPredicate(IBranch branch, IGitVersionConfiguration configuration)
    {
        private readonly IEnumerable<string> sourceBranchRegexes = GetSourceBranchRegexes(branch, configuration);

        public bool IsSourceBranch(INamedReference sourceBranchCandidate)
        {
            if (Equals(sourceBranchCandidate, branch))
                return false;

            var branchName = sourceBranchCandidate.Name.WithoutOrigin;

            return this.sourceBranchRegexes.Any(regex => Regex.IsMatch(branchName, regex));
        }

        private static IEnumerable<string> GetSourceBranchRegexes(INamedReference branch, IGitVersionConfiguration configuration)
        {
            var currentBranchConfig = configuration.GetBranchConfiguration(branch.Name);
            if (currentBranchConfig.SourceBranches == null)
            {
                yield return ".*";
            }
            else
            {
                var branches = configuration.Branches;
                foreach (var sourceBranch in currentBranchConfig.SourceBranches)
                {
                    var regex = branches[sourceBranch].RegularExpression;
                    if (regex != null)
                        yield return regex;
                }
            }
        }
    }
}
