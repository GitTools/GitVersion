using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Common;
using GitVersion.Extensions;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion.VersionCalculation
{
    /// <summary>
    /// Active only when the branch is marked as IsDevelop.
    /// Two different algorithms (results are merged):
    /// <para>
    /// Using <see cref="VersionInBranchNameVersionStrategy"/>:
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
    public class TrackReleaseBranchesVersionStrategy : VersionStrategyBase
    {
        private readonly IGitRepoMetadataProvider gitRepoMetadataProvider;
        private readonly VersionInBranchNameVersionStrategy releaseVersionStrategy;
        private readonly TaggedCommitVersionStrategy taggedCommitVersionStrategy;

        public TrackReleaseBranchesVersionStrategy(IGitRepoMetadataProvider gitRepoMetadataProvider, IOptions<GitVersionContext> versionContext) : base(versionContext)
        {
            this.gitRepoMetadataProvider = gitRepoMetadataProvider ?? throw new ArgumentNullException(nameof(gitRepoMetadataProvider));

            releaseVersionStrategy = new VersionInBranchNameVersionStrategy(gitRepoMetadataProvider, versionContext);
            taggedCommitVersionStrategy = new TaggedCommitVersionStrategy(gitRepoMetadataProvider, versionContext);
        }

        public override IEnumerable<BaseVersion> GetVersions()
        {
            if (Context.Configuration.TracksReleaseBranches)
            {
                return ReleaseBranchBaseVersions(Context).Union(MasterTagsVersions(Context));
            }

            return new BaseVersion[0];
        }

        private IEnumerable<BaseVersion> MasterTagsVersions(GitVersionContext context)
        {
            var master = context.Repository.FindBranch("master");
            if (master != null)
            {
                return taggedCommitVersionStrategy.GetTaggedVersions(master, null);
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
                    .Where(b => releaseBranchConfig.Any(c => Regex.IsMatch(b.FriendlyName, c.Value.Regex)));

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

        private IEnumerable<BaseVersion> GetReleaseVersion(GitVersionContext context, Branch releaseBranch)
        {
            var tagPrefixRegex = context.Configuration.GitTagPrefix;

            // Find the commit where the child branch was created.
            var baseSource = gitRepoMetadataProvider.FindMergeBase(releaseBranch, context.CurrentBranch);
            if (baseSource == context.CurrentCommit)
            {
                // Ignore the branch if it has no commits.
                return new BaseVersion[0];
            }

            return releaseVersionStrategy
                .GetVersions(tagPrefixRegex, releaseBranch)
                .Select(b => new BaseVersion(b.Source, true, b.SemanticVersion, baseSource, b.BranchNameOverride));
        }
    }
}
