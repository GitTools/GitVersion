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
            using (Logger.IndentLog("Building information about your Git repository"))
            {
                Logger.WriteInfo($"Current branch is {context.CurrentBranch.FriendlyName}");
                if (context.CurrentBranch.Tip != context.CurrentCommit)
                {
                    Logger.WriteInfo($@"Running against older commit of {
                        context.CurrentBranch.FriendlyName}, Tip: {
                        context.CurrentBranch.Tip.Sha.Substring(0, 8)}, Target: {
                        context.CurrentCommit.Sha.Substring(0, 8)}");
                }
                var tags = ReadRepoTags(context);
                var currentBranchInfo = ReadBranchInfo(context, context.CurrentBranch, context.CurrentCommit, tags);
                var releaseBranches = ReadReleaseBranches(context, tags, currentBranchInfo);
                var masterBranch = ReadMasterBranch(context, tags);

                return new GitRepoMetadata(
                    currentBranchInfo,
                    masterBranch,
                    releaseBranches);
            }
        }

        private static List<MBranch> ReadReleaseBranches(GitVersionContext context, List<MTag> allTags, MBranch currentBranch)
        {
            using (Logger.IndentLog("Building information about release branches"))
            {
                var releaseBranchConfig = context.FullConfiguration.Branches
                .Where(b => b.Value.IsReleaseBranch == true)
                .ToList();

                var releaseBranches = context.Repository
                    .Branches
                    .Where(b => releaseBranchConfig.Any(c => Regex.IsMatch(b.FriendlyName, c.Key)))
                    .ToList();

                Logger.WriteInfo($"Found {string.Join(", ", releaseBranches.Select(b => b.FriendlyName))}");
                return releaseBranches
                    .Select(b =>
                    {
                        // If current branch is a release branch, don't calculate everything again
                        if (b.FriendlyName == currentBranch.Name)
                        {
                            return currentBranch;
                        }

                        return ReadBranchInfo(context, b, b.Tip, allTags);
                    })
                    .ToList();
            }
        }

        private static List<MTag> ReadRepoTags(GitVersionContext context)
        {
            var olderThan = context.CurrentCommit.When();
            return context.Repository
                .Tags
                .Select(tag =>
                {
                    var commit = tag.PeeledTarget as Commit;
                    if (commit == null)
                    {
                        return null;
                    }
                    var tagDistance = context.RepositoryMetadataProvider.GetCommitCount(
                        context.CurrentCommit,
                        context.Repository.Lookup<Commit>(tag.PeeledTarget.Sha));

                    return new MTag(tag.FriendlyName, new MCommit(commit, tagDistance), context.FullConfiguration);
                })
                .Where(t => t != null)
                .ToList();
        }

        static MBranch ReadMasterBranch(GitVersionContext context, List<MTag> tags)
        {
            var masterBranch = context.Repository.Branches["master"];
            if (masterBranch == null)
            {
                return null;
            }
            return ReadBranchInfo(context, masterBranch, masterBranch.Tip, tags);
        }

        private static MBranch ReadBranchInfo(GitVersionContext context, Branch branch, Commit at, List<MTag> allTags)
        {
            using (Logger.IndentLog($"Calculating branch information for {branch.FriendlyName}"))
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
                    branchTags.AddRange(allTags.Where(t => t.Commit.Sha == branchCommit.Sha));
                    commitCount++;
                }

                var mbranch = new MBranch(branch.FriendlyName, tipCommit, lastCommit, new MParent(parentMCommit), new List<MBranchTag>(), mergeMessages);
                mbranch.Tags.AddRange(branchTags.Select(t => new MBranchTag(mbranch, t)));
                return mbranch;
            }
        }
    }
}
