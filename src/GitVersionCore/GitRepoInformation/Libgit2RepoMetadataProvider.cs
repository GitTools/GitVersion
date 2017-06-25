using LibGit2Sharp;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace GitVersion.GitRepoInformation
{
    public class Libgit2RepoMetadataProvider
    {
        public static GitRepoMetadata ReadMetadata(GitVersionContext context)
        {
            var tags = ReadRepoTags(context);
            var currentBranchInfo = ReadBranchInfo(context, context.CurrentBranch, context.CurrentCommit, tags);
            var releaseBranches = ReadReleaseBranches(context, tags);
            var masterBranch = context.Repository.Branches["master"];
            var masterBranchInfo = masterBranch != null ? ReadBranchInfo(context, masterBranch, masterBranch.Tip, tags) : null;
            return new GitRepoMetadata(
                currentBranchInfo,
                masterBranchInfo,
                releaseBranches);
        }

        private static List<MBranch> ReadReleaseBranches(GitVersionContext context, List<Tag> allTags)
        {
            var releaseBranchConfig = context.FullConfiguration.Branches
                .Where(b => b.Value.IsReleaseBranch == true)
                .ToList();

            return context.Repository
                .Branches
                .Where(b => releaseBranchConfig.Any(c => Regex.IsMatch(b.FriendlyName, c.Key)))
                .Select(b => ReadBranchInfo(context, b, b.Tip, allTags))
                .ToList();
        }

        private static List<Tag> ReadRepoTags(GitVersionContext context)
        {
            var olderThan = context.CurrentCommit.When();
            return context.Repository
                .Tags
                .Where(tag =>
                {
                    var commit = tag.PeeledTarget() as Commit;
                    return commit != null;
                })
                .ToList();
        }

        private static MBranch ReadBranchInfo(GitVersionContext context, Branch branch, Commit at, List<Tag> allTags)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = at ?? branch.Tip,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            var commitCount = 0;
            var mergeMessages = new List<MergeMessage>();
            var branchTags = new List<MTag>();
            var commits = context.Repository.Commits.QueryBy(filter);
            var parent = context.RepositoryMetadataProvider.FindCommitBranchWasBranchedFrom(branch);
            MCommit tipCommit = null;
            MCommit lastCommit = null;
            MCommit parentMCommit = null;
            foreach (var branchCommit in commits)
            {
                if (tipCommit == null)
                {
                    tipCommit = new MCommit(branchCommit, commitCount);
                }
                lastCommit = new MCommit(branchCommit, commitCount);
                if (branchCommit.Parents.Count() >= 2)
                {
                    mergeMessages.Add(new MergeMessage(lastCommit, context.FullConfiguration));
                }
                if (parent != BranchCommit.Empty && branchCommit.Sha == parent.Commit.Sha)
                {
                    parentMCommit = new MCommit(parent.Commit, commitCount);
                }

                // Adding range because the same commit may have two tags
                branchTags.AddRange(allTags
                    .Where(t => t.PeeledTarget.Sha == branchCommit.Sha)
                    .Select(t => new MTag(t.FriendlyName, new MCommit((Commit)t.PeeledTarget, commitCount), context.FullConfiguration)));
                commitCount++;
            }

            var mbranch = new MBranch(branch.FriendlyName, tipCommit, lastCommit, new MParent(parentMCommit), new List<MBranchTag>(), mergeMessages);
            mbranch.Tags.AddRange(branchTags.Select(t => new MBranchTag(mbranch, t)));
            return mbranch;
        }
    }
}
