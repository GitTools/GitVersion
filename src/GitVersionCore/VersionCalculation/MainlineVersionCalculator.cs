using GitVersion.VersionCalculation.BaseVersionCalculators;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitVersion.VersionCalculation
{
    class MainlineVersionCalculator
    {
        IMetaDataCalculator metaDataCalculator;

        public MainlineVersionCalculator(IMetaDataCalculator metaDataCalculator)
        {
            this.metaDataCalculator = metaDataCalculator;
        }

        public SemanticVersion FindMainlineModeVersion(BaseVersion baseVersion, GitVersionContext context)
        {
            if (baseVersion.SemanticVersion.PreReleaseTag.HasTag())
            {
                throw new NotSupportedException("Mainline development mode doesn't yet support pre-release tags on master");
            }

            using (Logger.IndentLog("Using mainline development mode to calculate current version"))
            {
                var mainlineVersion = baseVersion.SemanticVersion;

                // Forward merge / PR
                //          * feature/foo
                //         / |
                // master *  *
                // 

                var mainlineTip = GetMainlineTip(context);
                var commitsNotOnMainline = context.Repository.Commits.QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = context.CurrentBranch,
                    ExcludeReachableFrom = mainlineTip,
                    SortBy = CommitSortStrategies.Reverse,
                    FirstParentOnly = true
                }).Where(c => c.Sha != baseVersion.BaseVersionSource.Sha && c.Parents.Count() == 1).ToList();
                var commitLog = context.Repository.Commits.QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = context.CurrentBranch,
                    ExcludeReachableFrom = baseVersion.BaseVersionSource,
                    SortBy = CommitSortStrategies.Reverse,
                    FirstParentOnly = true
                })
                .Where(c => c.Sha != baseVersion.BaseVersionSource.Sha)
                .Except(commitsNotOnMainline)
                .ToList();

                var directCommits = new List<Commit>();

                // Scans commit log in reverse, aggregating merge commits
                foreach (var commit in commitLog)
                {
                    directCommits.Add(commit);
                    if (commit.Parents.Count() > 1)
                    {
                        mainlineVersion = AggregateMergeCommitIncrement(context, commit, directCommits, mainlineVersion);
                    }
                }

                if (context.CurrentBranch.FriendlyName != "master")
                {
                    var mergedHead = context.CurrentCommit;
                    var findMergeBase = context.Repository.ObjectDatabase.FindMergeBase(context.CurrentCommit, mainlineTip);
                    Logger.WriteInfo(string.Format("Current branch ({0}) was branch from {1}", context.CurrentBranch.FriendlyName, findMergeBase));

                    var branchIncrement = FindMessageIncrement(context, null, mergedHead, findMergeBase, directCommits);
                    // This will increment for any direct commits on master
                    mainlineVersion = IncrementForEachCommit(context, directCommits, mainlineVersion, "master");
                    mainlineVersion.BuildMetaData = metaDataCalculator.Create(findMergeBase, context);
                    // Don't increment if the merge commit is a merge into mainline
                    // this ensures PR's and forward merges end up correct.
                    if (mergedHead.Parents.Count() == 1 || mergedHead.Parents.First() != mainlineTip)
                    {
                        Logger.WriteInfo(string.Format("Performing {0} increment for current branch ", branchIncrement));
                        mainlineVersion = mainlineVersion.IncrementVersion(branchIncrement);
                    }
                }
                else
                {
                    // If we are on master, make sure no commits get left behind
                    mainlineVersion = IncrementForEachCommit(context, directCommits, mainlineVersion);
                    mainlineVersion.BuildMetaData = metaDataCalculator.Create(baseVersion.BaseVersionSource, context);
                }

                return mainlineVersion;
            }
        }

        SemanticVersion AggregateMergeCommitIncrement(GitVersionContext context, Commit commit, List<Commit> directCommits, SemanticVersion mainlineVersion)
        {
            // Merge commit, process all merged commits as a batch
            var mergeCommit = commit;
            var mergedHead = GetMergedHead(mergeCommit);
            var findMergeBase = context.Repository.ObjectDatabase.FindMergeBase(mergeCommit.Parents.First(), mergedHead);
            var findMessageIncrement = FindMessageIncrement(context, mergeCommit, mergedHead, findMergeBase, directCommits);

            // If this collection is not empty there has been some direct commits against master
            // Treat each commit as it's own 'release', we need to do this before we increment the branch
            mainlineVersion = IncrementForEachCommit(context, directCommits, mainlineVersion);
            directCommits.Clear();

            // Finally increment for the branch
            mainlineVersion = mainlineVersion.IncrementVersion(findMessageIncrement);
            Logger.WriteInfo(string.Format("Merge commit {0} incremented base versions {1}, now {2}",
                mergeCommit.Sha, findMessageIncrement, mainlineVersion));
            return mainlineVersion;
        }

        static Commit GetMainlineTip(GitVersionContext context)
        {
            var mainlineBranchConfigs = context.FullConfiguration.Branches.Where(b => b.Value.IsMainline == true).ToList();
            var seenMainlineTips = new List<string>();
            var mainlineBranches = context.Repository.Branches
                .Where(b =>
                {
                    return mainlineBranchConfigs.Any(c => Regex.IsMatch(b.FriendlyName, c.Key));
                })
                .Where(b =>
                {
                    if (seenMainlineTips.Contains(b.Tip.Sha))
                    {
                        Logger.WriteInfo("Multiple possible mainlines pointing at the same commit, dropping " + b.FriendlyName);
                        return false;
                    }
                    seenMainlineTips.Add(b.Tip.Sha);
                    return true;
                })
                .Select(b => new
                {
                    MergeBase = context.Repository.ObjectDatabase.FindMergeBase(b.Tip, context.CurrentCommit),
                    Branch = b
                })
                .Where(a => a.MergeBase != null)
                .GroupBy(b => b.MergeBase.Sha, b => b.Branch)
                .ToDictionary(b => b.Key, b => b.ToList());

            var allMainlines = mainlineBranches.Values.SelectMany(branches => branches.Select(b => b.FriendlyName));
            Logger.WriteInfo("Found possible mainline branches: " + string.Join(", ", allMainlines));

            // Find closest mainline branch
            var firstMatchingCommit = context.CurrentBranch.Commits.First(c => mainlineBranches.ContainsKey(c.Sha));
            var possibleMainlineBranches = mainlineBranches[firstMatchingCommit.Sha];

            if (possibleMainlineBranches.Count == 1)
            {
                var mainlineBranch = possibleMainlineBranches[0];
                Logger.WriteInfo("Mainline for current branch is " + mainlineBranch.FriendlyName);
                return mainlineBranch.Tip;
            }

            var chosenMainline = possibleMainlineBranches[0];
            Logger.WriteInfo(string.Format(
                "Multiple mainlines ({0}) have the same merge base for the current branch, choosing {1} because we found that branch first...",
                string.Join(", ", possibleMainlineBranches.Select(b => b.FriendlyName)),
                chosenMainline.FriendlyName));
            return chosenMainline.Tip;
        }

        private static SemanticVersion IncrementForEachCommit(GitVersionContext context, List<Commit> directCommits, SemanticVersion mainlineVersion, string branch = null)
        {
            foreach (var directCommit in directCommits)
            {
                var directCommitIncrement = IncrementStrategyFinder.GetIncrementForCommits(context, new[]
                                            {
                                                directCommit
                                            }) ?? IncrementStrategyFinder.FindDefaultIncrementForBranch(context, branch);
                mainlineVersion = mainlineVersion.IncrementVersion(directCommitIncrement);
                Logger.WriteInfo(string.Format("Direct commit on master {0} incremented base versions {1}, now {2}",
                    directCommit.Sha, directCommitIncrement, mainlineVersion));
            }
            return mainlineVersion;
        }

        private static VersionField FindMessageIncrement(
            GitVersionContext context, Commit mergeCommit, Commit mergedHead, Commit findMergeBase, List<Commit> commitLog)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = mergedHead,
                ExcludeReachableFrom = findMergeBase
            };
            var commits = mergeCommit == null ?
                context.Repository.Commits.QueryBy(filter).ToList() :
                new[] { mergeCommit }.Union(context.Repository.Commits.QueryBy(filter)).ToList();
            commitLog.RemoveAll(c => commits.Any(c1 => c1.Sha == c.Sha));
            return IncrementStrategyFinder.GetIncrementForCommits(context, commits)
                ?? TryFindIncrementFromMergeMessage(mergeCommit, context);
        }

        private static VersionField TryFindIncrementFromMergeMessage(Commit mergeCommit, GitVersionContext context)
        {
            if (mergeCommit != null)
            {
                var mergeMessage = new MergeMessage(mergeCommit.Message, context.FullConfiguration);
                if (mergeMessage.MergedBranch != null)
                {
                    var config = context.FullConfiguration.GetConfigForBranch(mergeMessage.MergedBranch);
                    if (config != null && config.Increment.HasValue && config.Increment != IncrementStrategy.Inherit)
                    {
                        return config.Increment.Value.ToVersionField();
                    }
                }
            }

            // Fallback to config increment value
            return IncrementStrategyFinder.FindDefaultIncrementForBranch(context);
        }

        private Commit GetMergedHead(Commit mergeCommit)
        {
            var parents = mergeCommit.Parents.Skip(1).ToList();
            if (parents.Count > 1)
                throw new NotSupportedException("Mainline development does not support more than one merge source in a single commit yet");
            return parents.Single();
        }
    }
}
