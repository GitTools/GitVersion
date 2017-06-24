using System;
using LibGit2Sharp;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using System.Linq;

namespace GitVersion.GitRepoInformation
{
    public class Libgit2RepoMetadataProvider
    {
        public static GitRepoMetadata ReadMetadata(GitVersionContext context)
        {
            return new GitRepoMetadata(
                ReadCurrentBranchInfo(context));
        }

        private static MCurrentBranch ReadCurrentBranchInfo(GitVersionContext context)
        {
            var branchCommits = context.CurrentBranch.CommitsPriorToThan(context.CurrentCommit.When());
            if (branchCommits.First() != context.CurrentCommit)
            {
                throw new Exception("Doesn't include first commit");
            }
            var mergeMessages = branchCommits
                .SelectMany(c =>
                {
                    if (c.Parents.Count() < 2)
                    {
                        return Enumerable.Empty<MergeMessage>();
                    }

                    return new[]
                    {
                        new MergeMessage(c.Message, c.Sha, context.FullConfiguration)
                    };
                }).ToList();

            return new MCurrentBranch(mergeMessages);
        }
    }
}
