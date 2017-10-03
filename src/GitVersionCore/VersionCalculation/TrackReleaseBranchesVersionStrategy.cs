namespace GitVersion.VersionCalculation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BaseVersionCalculators;
    using GitTools;
    using LibGit2Sharp;
    using System;
    using GitVersion.GitRepoInformation;

    /// <summary>
    /// Active only when the branch is marked as IsDevelop.
    /// Two different algorithms (results are merged):
    /// <para>
    /// Using <see cref="VersionInBranchNameBaseVersionStrategy"/>:
    /// Version is that of any child branches marked with IsReleaseBranch (except if they have no commits of their own).
    /// BaseVersionSource is the commit where the child branch was created.
    /// Always increments.
    /// </para>
    /// <para>
    /// Using <see cref="TaggedCommitVersionStrategy"/>:
    /// Version is extracted from all tags on the <c>master</c> branch which are valid.
    /// BaseVersionSource is the tag's commit (same as base strategy).
    /// Increments if the tag is not the current commit (same as base strategy).
    /// </para>
    /// </summary>
    public class TrackReleaseBranchesVersionStrategy : BaseVersionStrategy
    {
        VersionInBranchNameBaseVersionStrategy releaseVersionStrategy = new VersionInBranchNameBaseVersionStrategy();
        TaggedCommitVersionStrategy taggedCommitVersionStrategy = new TaggedCommitVersionStrategy();
        Func<IBaseVersionCalculator> getBaseVersionCalculator;

        public TrackReleaseBranchesVersionStrategy(Func<IBaseVersionCalculator> getBaseVersionCalculator)
        {
            this.getBaseVersionCalculator = getBaseVersionCalculator;
        }

        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            if (context.Configuration.TracksReleaseBranches)
            {


                // I feel this is actually a recursive path for GitVersion and rather than
                // having all this logic in here, we should just run it for each release branch and master

                // Something like this, but will need to refactor BaseVersionStrategy
                //var baseVersionCalculator = getBaseVersionCalculator();
                //context.RepositoryMetadata.ReleaseBranches
                //    .Select(r => baseVersionCalculator.GetBaseVersion()


                return ReleaseBranchBaseVersions(context).Union(MasterTagsVersions(context));
            }

            return new BaseVersion[0];
        }

        private IEnumerable<BaseVersion> MasterTagsVersions(GitVersionContext context)
        {
            var master = context.RepositoryMetadata.MasterBranch;
            if (master != null)
            {
                return taggedCommitVersionStrategy.GetTaggedVersions(context, master);
            }

            return new BaseVersion[0];
        }

        private IEnumerable<BaseVersion> ReleaseBranchBaseVersions(GitVersionContext context)
        {
            return context
                .RepositoryMetadata
                .ReleaseBranches
                .SelectMany(b => GetReleaseVersion(context, b))
                .Select(baseVersion =>
                {
                    // Need to drop branch overrides and give a bit more context about
                    // where this version came from
                    var source1 = "Release branch exists -> " + baseVersion.Source;
                    return new BaseVersion(context,
                        source1,
                        baseVersion.ShouldIncrement,
                        baseVersion.SemanticVersion,
                        baseVersion.BaseVersionSource,
                        null);
                })
                .ToList();
        }

        IEnumerable<BaseVersion> GetReleaseVersion(GitVersionContext context, MBranch releaseBranch)
        {
            var tagPrefixRegex = context.Configuration.GitTagPrefix;
            var repository = context.Repository;

            // Find the commit where the child branch was created.
            var baseSource = context.RepositoryMetadataProvider.FindMergeBase(releaseBranch.TipSha, context.CurrentCommit.Sha);
            if (baseSource == context.CurrentCommit)
            {
                // Ignore the branch if it has no commits.
                return new BaseVersion[0];
            }

            return releaseVersionStrategy
                .GetVersions(context, tagPrefixRegex, releaseBranch, repository)
                .Select(b => new BaseVersion(context, b.Source, true, b.SemanticVersion, baseSource, b.BranchNameOverride));
        }
    }
}