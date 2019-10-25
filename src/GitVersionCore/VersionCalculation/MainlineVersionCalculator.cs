using GitVersion.VersionCalculation.BaseVersionCalculators;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Logging;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation
{
    internal class MainlineVersionCalculator : IMainlineVersionCalculator
    {
        private readonly IMetaDataCalculator metaDataCalculator;
        private readonly ILog log;

        public MainlineVersionCalculator(ILog log, IMetaDataCalculator metaDataCalculator)
        {
            this.metaDataCalculator = metaDataCalculator ?? throw new ArgumentNullException(nameof(metaDataCalculator));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public SemanticVersion FindMainlineModeVersion(BaseVersion baseVersion, GitVersionContext context)
        {
            if (baseVersion.SemanticVersion.PreReleaseTag.HasTag())
            {
                throw new NotSupportedException("Mainline development mode doesn't yet support pre-release tags on master");
            }

            using (log.IndentLog("Using mainline development mode to calculate current version"))
            {
                var mainlineVersion = baseVersion.SemanticVersion;

                // Forward merge / PR
                //          * feature/foo
                //         / |
                // master *  *
                // 

                var mergeBase = baseVersion.BaseVersionSource;
                var mainline = GetMainline(context, baseVersion.BaseVersionSource);
                var mainlineTip = mainline.Tip;

                // when the current branch is not mainline, find the effective mainline tip for versioning the branch
                if (!context.CurrentBranch.IsSameBranch(mainline))
                {
                    mergeBase = FindMergeBaseBeforeForwardMerge(context, baseVersion.BaseVersionSource, mainline, out mainlineTip);
                    log.Info($"Current branch ({context.CurrentBranch.FriendlyName}) was branch from {mergeBase}");
                }

                var mainlineCommitLog = context.Repository.Commits.QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = mainlineTip,
                    ExcludeReachableFrom = baseVersion.BaseVersionSource,
                    SortBy = CommitSortStrategies.Reverse,
                    FirstParentOnly = true
                })
                .ToList();
                var directCommits = new List<Commit>(mainlineCommitLog.Count);

                // Scans commit log in reverse, aggregating merge commits
                foreach (var commit in mainlineCommitLog)
                {
                    directCommits.Add(commit);
                    if (commit.Parents.Count() > 1)
                    {
                        mainlineVersion = AggregateMergeCommitIncrement(context, commit, directCommits, mainlineVersion, mainline);
                    }
                }

                // This will increment for any direct commits on mainline
                mainlineVersion = IncrementForEachCommit(context, directCommits, mainlineVersion, mainline);
                mainlineVersion.BuildMetaData = metaDataCalculator.Create(mergeBase, context);

                // branches other than master always get a bump for the act of branching
                if (context.CurrentBranch.FriendlyName != "master")
                {
                    var branchIncrement = FindMessageIncrement(context, null, context.CurrentCommit, mergeBase, mainlineCommitLog);
                    log.Info($"Performing {branchIncrement} increment for current branch ");

                    mainlineVersion = mainlineVersion.IncrementVersion(branchIncrement);
                }

                return mainlineVersion;
            }
        }

        private SemanticVersion AggregateMergeCommitIncrement(GitVersionContext context, Commit commit, List<Commit> directCommits, SemanticVersion mainlineVersion, Branch mainline)
        {
            // Merge commit, process all merged commits as a batch
            var mergeCommit = commit;
            var mergedHead = GetMergedHead(mergeCommit);
            var findMergeBase = context.Repository.ObjectDatabase.FindMergeBase(mergeCommit.Parents.First(), mergedHead);
            var findMessageIncrement = FindMessageIncrement(context, mergeCommit, mergedHead, findMergeBase, directCommits);

            // If this collection is not empty there has been some direct commits against master
            // Treat each commit as it's own 'release', we need to do this before we increment the branch
            mainlineVersion = IncrementForEachCommit(context, directCommits, mainlineVersion, mainline);
            directCommits.Clear();

            // Finally increment for the branch
            mainlineVersion = mainlineVersion.IncrementVersion(findMessageIncrement);
            log.Info($"Merge commit {mergeCommit.Sha} incremented base versions {findMessageIncrement}, now {mainlineVersion}");
            return mainlineVersion;
        }

        private Branch GetMainline(GitVersionContext context, Commit baseVersionSource)
        {
            var mainlineBranchConfigs = context.FullConfiguration.Branches.Where(b => b.Value.IsMainline == true).ToList();
            var mainlineBranches = context.Repository.Branches
                .Where(b =>
                {
                    return mainlineBranchConfigs.Any(c => Regex.IsMatch(b.FriendlyName, c.Value.Regex));
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
            log.Info("Found possible mainline branches: " + string.Join(", ", allMainlines));

            // Find closest mainline branch
            var firstMatchingCommit = context.CurrentBranch.Commits.First(c => mainlineBranches.ContainsKey(c.Sha));
            var possibleMainlineBranches = mainlineBranches[firstMatchingCommit.Sha];

            if (possibleMainlineBranches.Count == 1)
            {
                var mainlineBranch = possibleMainlineBranches[0];
                log.Info("Mainline for current branch is " + mainlineBranch.FriendlyName);
                return mainlineBranch;
            }

            // prefer current branch, if it is a mainline branch
            if (possibleMainlineBranches.Any(context.CurrentBranch.IsSameBranch))
            {
                log.Info($"Choosing {context.CurrentBranch.FriendlyName} as mainline because it is the current branch");
                return context.CurrentBranch;
            }

            // prefer a branch on which the merge base was a direct commit, if there is such a branch
            var firstMatchingCommitBranch = possibleMainlineBranches
                .FirstOrDefault(b =>
                {
                    var filter = new CommitFilter
                    {
                        IncludeReachableFrom = b,
                        ExcludeReachableFrom = baseVersionSource,
                        FirstParentOnly = true,
                    };
                    var query = context.Repository.Commits.QueryBy(filter);

                    return query.Contains(firstMatchingCommit);
                });
            if (firstMatchingCommitBranch != null)
            {
                var message = string.Format(
                    "Choosing {0} as mainline because {1}'s merge base was a direct commit to {0}",
                    firstMatchingCommitBranch.FriendlyName,
                    context.CurrentBranch.FriendlyName);
                log.Info(message);

                return firstMatchingCommitBranch;
            }

            var chosenMainline = possibleMainlineBranches[0];
            log.Info($"Multiple mainlines ({string.Join(", ", possibleMainlineBranches.Select(b => b.FriendlyName))}) have the same merge base for the current branch, choosing {chosenMainline.FriendlyName} because we found that branch first...");
            return chosenMainline;
        }

        /// <summary>
        /// Gets the commit on mainline at which <paramref name="mergeBase"/> was fully integrated.
        /// </summary>
        /// <param name="mainlineCommitLog">The collection of commits made directly to mainline, in reverse order.</param>
        /// <param name="mergeBase">The best possible merge base between <paramref name="mainlineTip"/> and the current commit.</param>
        /// <param name="mainlineTip">The tip of the mainline branch.</param>
        /// <returns>The commit on mainline at which <paramref name="mergeBase"/> was merged, if such a commit exists; otherwise, <paramref name="mainlineTip"/>.</returns>
        /// <remarks>
        /// This method gets the most recent commit on mainline that should be considered for versioning the current branch.
        /// </remarks>
        private Commit GetEffectiveMainlineTip(IEnumerable<Commit> mainlineCommitLog, Commit mergeBase, Commit mainlineTip)
        {
            // find the commit that merged mergeBase into mainline
            foreach (var commit in mainlineCommitLog)
            {
                if (commit == mergeBase || commit.Parents.Contains(mergeBase))
                {
                    log.Info($"Found branch merge point; choosing {commit} as effective mainline tip");
                    return commit;
                }
            }

            return mainlineTip;
        }

        /// <summary>
        /// Gets the best possible merge base between the current commit and <paramref name="mainline"/> that is not the child of a forward merge.
        /// </summary>
        /// <param name="context">The current versioning context.</param>
        /// <param name="baseVersionSource">The commit that establishes the contextual base version.</param>
        /// <param name="mainline">The mainline branch.</param>
        /// <param name="mainlineTip">The commit on mainline at which the returned merge base was fully integrated.</param>
        /// <returns>The best possible merge base between the current commit and <paramref name="mainline"/> that is not the child of a forward merge.</returns>
        private Commit FindMergeBaseBeforeForwardMerge(GitVersionContext context, Commit baseVersionSource, Branch mainline, out Commit mainlineTip)
        {
            var mergeBase = context.Repository.ObjectDatabase.FindMergeBase(context.CurrentCommit, mainline.Tip);
            var mainlineCommitLog = context.Repository.Commits
                .QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = mainline.Tip,
                    ExcludeReachableFrom = baseVersionSource,
                    SortBy = CommitSortStrategies.Reverse,
                    FirstParentOnly = true
                })
                .ToList();

            // find the mainline commit effective for versioning the current branch
            mainlineTip = GetEffectiveMainlineTip(mainlineCommitLog, mergeBase, mainline.Tip);

            // detect forward merge and rewind mainlineTip to before it
            if (mergeBase == context.CurrentCommit && !mainlineCommitLog.Contains(mergeBase))
            {
                var mainlineTipPrevious = mainlineTip.Parents.First();
                var message = $"Detected forward merge at {mainlineTip}; rewinding mainline to previous commit {mainlineTipPrevious}";

                log.Info(message);

                // re-do mergeBase detection before the forward merge
                mergeBase = context.Repository.ObjectDatabase.FindMergeBase(context.CurrentCommit, mainlineTipPrevious);
                mainlineTip = GetEffectiveMainlineTip(mainlineCommitLog, mergeBase, mainlineTipPrevious);
            }

            return mergeBase;
        }

        private SemanticVersion IncrementForEachCommit(GitVersionContext context, List<Commit> directCommits, SemanticVersion mainlineVersion, Branch mainline)
        {
            foreach (var directCommit in directCommits)
            {
                var directCommitIncrement = IncrementStrategyFinder.GetIncrementForCommits(context, new[]
                                            {
                                                directCommit
                                            }) ?? IncrementStrategyFinder.FindDefaultIncrementForBranch(context, mainline.FriendlyName);
                mainlineVersion = mainlineVersion.IncrementVersion(directCommitIncrement);
                log.Info($"Direct commit on master {directCommit.Sha} incremented base versions {directCommitIncrement}, now {mainlineVersion}");
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
                    if (config?.Increment != null && config.Increment != IncrementStrategy.Inherit)
                    {
                        return config.Increment.Value.ToVersionField();
                    }
                }
            }

            // Fallback to config increment value
            return IncrementStrategyFinder.FindDefaultIncrementForBranch(context);
        }

        private static Commit GetMergedHead(Commit mergeCommit)
        {
            var parents = mergeCommit.Parents.Skip(1).ToList();
            if (parents.Count > 1)
                throw new NotSupportedException("Mainline development does not support more than one merge source in a single commit yet");
            return parents.Single();
        }
    }
}
