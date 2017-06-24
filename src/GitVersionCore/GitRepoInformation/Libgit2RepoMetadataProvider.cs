using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitVersion.GitRepoInformation
{
    public class Libgit2RepoMetadataProvider
    {
        public static GitRepoMetadata ReadMetadata(GitVersionContext context)
        {
            var tags = ReadRepoTags(context);
            return new GitRepoMetadata(
                tags,
                ReadCurrentBranchInfo(context, tags));
        }

        private static List<MTag> ReadRepoTags(GitVersionContext context)
        {
            var olderThan = context.CurrentCommit.When();
            return context.Repository
                .Tags
                .Where(tag =>
                {
                    var commit = tag.PeeledTarget() as Commit;
                    if (commit != null)
                    {
                        return commit.When() <= olderThan;
                    }
                    return false;
                })
                .Select(gitTag => new MTag(gitTag.Target.Sha, gitTag.FriendlyName, context.FullConfiguration))
                .ToList();
        }

        private static MCurrentBranch ReadCurrentBranchInfo(GitVersionContext context, List<MTag> tags)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = context.CurrentCommit
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
                branchTags.AddRange(tags.Where(t => t.Sha == branchCommit.Sha));
            }

            return new MCurrentBranch(mergeMessages, branchTags);
        }
    }
}
