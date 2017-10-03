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
                tags,
                currentBranchInfo,
                masterBranchInfo,
                releaseBranches);
        }

        private static List<MBranch> ReadReleaseBranches(GitVersionContext context, List<MTag> allTags)
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

        private static List<MTag> ReadRepoTags(GitVersionContext context)
        {
            var olderThan = context.CurrentCommit.When();
            return context.Repository
                .Tags
                .Where(tag =>
                {
                    var commit = tag.PeeledTarget() as Commit;
                    return commit != null;
                })
                .Select(gitTag =>
                {
                    var commit = gitTag.PeeledTarget() as Commit;
                    if (commit == null) return null;

                    return new MTag(gitTag.Target.Sha, gitTag.FriendlyName, context.FullConfiguration, commit.When() > olderThan);
                })
                .Where(t => t != null)
                .ToList();
        }

        private static MBranch ReadBranchInfo(GitVersionContext context, Branch branch, Commit at, List<MTag> allTags)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = at ?? branch.Tip
            };

            var mergeMessages = new List<MergeMessage>();
            var branchTags = new List<MTag>();
            var commits = context.Repository.Commits.QueryBy(filter);
            foreach (var branchCommit in commits)
            {
                if (branchCommit.Parents.Count() >= 2)
                {
                    mergeMessages.Add(new MergeMessage(branchCommit.Message, branchCommit.Sha, context.FullConfiguration));
                }

                // Adding range because the same commit may have two tags
                branchTags.AddRange(allTags.Where(t => t.Sha == branchCommit.Sha));
            }

            var parentCommit = context.RepositoryMetadataProvider.FindCommitBranchWasBranchedFrom(branch);
            var parent = parentCommit == null || parentCommit.Commit == null
                ? null
                : new MParent(parentCommit.Commit.Sha);
            return new MBranch(branch.FriendlyName, at.Sha, parent, branchTags, mergeMessages);
        }
    }
}
