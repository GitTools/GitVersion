namespace GitVersion.VersionCalculation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BaseVersionCalculators;
    using GitTools;
    using LibGit2Sharp;

    /// <summary>
    /// Inherit version from release branch and tags on master
    /// </summary>
    public class DevelopVersionStrategy : BaseVersionStrategy
    {
        VersionInBranchBaseVersionStrategy releaseVersionStrategy = new VersionInBranchBaseVersionStrategy();
        TaggedCommitVersionStrategy taggedCommitVersionStrategy = new TaggedCommitVersionStrategy();

        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            if (context.Configuration.IsCurrentBranchDevelop)
            {
                return ReleaseBranchBaseVersions(context).Union(MasterTagsVersions(context));
            }

            return new BaseVersion[0];
        }

        private IEnumerable<BaseVersion> MasterTagsVersions(GitVersionContext context)
        {
            var master = context.Repository.FindBranch("master");
            if (master != null)
            {
                return taggedCommitVersionStrategy.GetTaggedVersions(context, master, null);
            }

            return new BaseVersion[0];
        }

        private IEnumerable<BaseVersion> ReleaseBranchBaseVersions(GitVersionContext context)
        {
            var releaseBranchConfig = context.FullConfiguration.Branches
                .Where(b => b.Value.IsReleaseBranch == true)
                .ToList();
            if (releaseBranchConfig.Any())
            {
                var releaseBranches = context.Repository.Branches
                    .Where(b => releaseBranchConfig.Any(c => Regex.IsMatch(b.FriendlyName, c.Key)));

                return releaseBranches
                    .SelectMany(b => GetReleaseVersion(context, b))
                    .Select(baseVersion =>
                    {
                        // Need to drop branch overrides and give a bit more context about
                        // where this version came from
                        var source1 = "Release branch exists -> " + baseVersion.Source;
                        return new BaseVersion(source1,
                            baseVersion.ShouldIncrement,
                            baseVersion.SemanticVersion,
                            baseVersion.BaseVersionSource,
                            null);
                    })
                    .ToList();
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