namespace GitVersion.VersionStrategies
{
    using System.Linq;

    using GitVersion.Configuration;

    using LibGit2Sharp;

    public class LastTagVersionStrategy : VersionStrategyBase
    {
        public override SemanticVersion CalculateVersion(GitVersionContext context)
        {
            var lastTaggedCommit = this.GetLastTaggedCommit(context);
            if (lastTaggedCommit == null) return null;

            var version = lastTaggedCommit.SemVer;

            int numberOfCommitsSinceRelease;

            if (lastTaggedCommit.Commit != context.CurrentCommit || context.CurrentBranchConfig.IncrementOnTag)
            {
                switch (context.CurrentBranchConfig.IncrementType)
                {
                    case IncrementType.Major:
                        version.Major++;
                        version.Minor = 0;
                        version.Patch = 0;
                        version.PreReleaseTag = new SemanticVersionPreReleaseTag(context.CurrentBranchConfig.Tag, 1);
                        break;
                    case IncrementType.Minor:
                        version.Minor++;
                        version.Patch = 0;
                        version.PreReleaseTag = new SemanticVersionPreReleaseTag(context.CurrentBranchConfig.Tag, 1);
                        break;
                    case IncrementType.Patch:
                        version.Patch++;
                        version.PreReleaseTag = new SemanticVersionPreReleaseTag(context.CurrentBranchConfig.Tag, 1);
                        break;
                    case IncrementType.PreReleaseTag:
                        version.PreReleaseTag.Number++;
                        break;
                }

                var until = lastTaggedCommit.Commit;
                if (context.CurrentBranchConfig.ForceBuildMetdataFromReferenceBranch)
                {
                    until = context.Repository.FindBranch(context.CurrentBranchConfig.ReferenceBranch).Tip;
                }

                var f = new CommitFilter
                            {
                                Since = context.CurrentCommit,
                                Until = until,
                                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
                            };

                var c = context.Repository.Commits.QueryBy(f);
                numberOfCommitsSinceRelease = c.Count();
                if (!context.CurrentBranchConfig.ForceBuildMetdataFromReferenceBranch)
                {
                    numberOfCommitsSinceRelease--;
                }
            }
            else
            {
                var applicableTagsInDescendingOrder = context.Repository.SemVerTagsRelatedToVersion(context.Configuration, version).OrderByDescending(tag => SemanticVersion.Parse(tag.Name, context.Configuration.TagPrefix)).ToList();
                numberOfCommitsSinceRelease = BranchCommitDifferenceFinder.NumberOfCommitsSinceLastTagOrBranchPoint(context, applicableTagsInDescendingOrder, BranchType.Unknown, context.CurrentBranchConfig.ReferenceBranch);// (context, applicableTagsInDescendingOrder, BranchType.Release, context.CurrentBranchConfig.ReferenceBranch);
            }
            var tip = context.CurrentCommit;
            version.BuildMetaData = new SemanticVersionBuildMetaData(numberOfCommitsSinceRelease, context.CurrentBranch.Name, tip.Sha, tip.When());
            return version;
        }

        protected VersionTaggedCommit GetLastTaggedCommit(GitVersionContext context)
        {
            var branch = string.IsNullOrEmpty(context.CurrentBranchConfig.LastTagReferenceBranch) 
                ? context.CurrentBranch 
                : context.Repository.Branches[context.CurrentBranchConfig.LastTagReferenceBranch];

            var tags = context.Repository.Tags.Select(t =>
            {
                SemanticVersion version;
                if (SemanticVersion.TryParse(t.Name, context.Configuration.TagPrefix, out version))
                {
                    return new VersionTaggedCommit((Commit)t.Target, version, t);
                }
                return null;
            })
               .Where(a => a != null)
               .ToArray();
            var olderThan = context.CurrentCommit.When();
            var lastTaggedCommit =
                branch.Commits.FirstOrDefault(c => c.When() <= olderThan && tags.Any(a => a.Commit == c));

            if (lastTaggedCommit != null)
            {
                return tags.Last(a => a.Commit.Sha == lastTaggedCommit.Sha);
            }

            return null;
        }
    }
}
