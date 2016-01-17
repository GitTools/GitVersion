namespace GitVersion.VersionCalculation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using GitVersion.VersionCalculation.BaseVersionCalculators;
    using LibGit2Sharp;

    /// <summary>
    /// Inherit version from release branch
    /// </summary>
    public class DevelopVersionStrategy : BaseVersionStrategy
    {
        VersionInBranchBaseVersionStrategy releaseVersionStrategy = new VersionInBranchBaseVersionStrategy();

        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            if (context.Configuration.IsCurrentBranchDevelop)
            {
                var releaseBranchConfig = context.FullConfiguration.Branches
                    .Where(b => b.Value.IsReleaseBranch == true)
                    .ToList();
                if (releaseBranchConfig.Any())
                {
                    var releaseBranches = context.Repository.Branches
                        .Where(b => releaseBranchConfig.Any(c => Regex.IsMatch(b.FriendlyName, c.Key)));

                    var baseVersions = releaseBranches.SelectMany(b => GetReleaseVersion(context, b)).ToList();
                    return baseVersions;
                }
            }

            return new BaseVersion[0];
        }

        IEnumerable<BaseVersion> GetReleaseVersion(GitVersionContext context, Branch releaseBranch)
        {
            var tagPrefixRegex = context.Configuration.GitTagPrefix;
            var repository = context.Repository;
            var baseSource = releaseBranch.FindMergeBase(context.CurrentBranch, repository);
            return releaseVersionStrategy
                .GetVersions(tagPrefixRegex, releaseBranch, repository)
                .Select(b => new BaseVersion(b.Source, true, b.SemanticVersion, baseSource, b.BranchNameOverride));
        }
    }
}