namespace GitVersion.VersionCalculation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BaseVersionCalculators;
    using LibGit2Sharp;

    public class NextVersionCalculator
    {
        IBaseVersionCalculator baseVersionFinder;
        IMetaDataCalculator metaDataCalculator;

        public NextVersionCalculator(IBaseVersionCalculator baseVersionCalculator = null, IMetaDataCalculator metaDataCalculator = null)
        {
            this.metaDataCalculator = metaDataCalculator ?? new MetaDataCalculator();
            baseVersionFinder = baseVersionCalculator ??
                new BaseVersionCalculator(
                    new FallbackBaseVersionStrategy(),
                    new ConfigNextVersionBaseVersionStrategy(),
                    new TaggedCommitVersionStrategy(),
                    new MergeMessageBaseVersionStrategy(),
                    new VersionInBranchNameBaseVersionStrategy(),
                    new TrackReleaseBranchesVersionStrategy());
        }

        public SemanticVersion FindVersion(GitVersionContext context)
        {
            SemanticVersion taggedSemanticVersion = null;
            // If current commit is tagged, don't do anything except add build metadata
            if (context.IsCurrentCommitTagged)
            {
                // Will always be 0, don't bother with the +0 on tags
                var semanticVersionBuildMetaData = metaDataCalculator.Create(context.CurrentCommit, context);
                semanticVersionBuildMetaData.CommitsSinceTag = null;

                var semanticVersion = new SemanticVersion(context.CurrentCommitTaggedVersion)
                {
                    BuildMetaData = semanticVersionBuildMetaData
                };
                taggedSemanticVersion = semanticVersion;
            }

            var baseVersion = baseVersionFinder.GetBaseVersion(context);
            SemanticVersion semver;
            if (context.Configuration.VersioningMode == VersioningMode.Mainline)
                semver = FindMainlineModeVersion(baseVersion, context);
            else
            {
                semver = PerformIncrement(context, baseVersion);
                semver.BuildMetaData = metaDataCalculator.Create(baseVersion.BaseVersionSource, context);
            }

            if (!semver.PreReleaseTag.HasTag() && !string.IsNullOrEmpty(context.Configuration.Tag))
            {
                UpdatePreReleaseTag(context, semver, baseVersion.BranchNameOverride);
            }


            if (taggedSemanticVersion != null)
            {
                // set the commit count on the tagged ver
                taggedSemanticVersion.BuildMetaData.CommitsSinceVersionSource = semver.BuildMetaData.CommitsSinceVersionSource;
            }

            return taggedSemanticVersion ?? semver;
        }

        private static SemanticVersion PerformIncrement(GitVersionContext context, BaseVersion baseVersion)
        {
            var semver = baseVersion.SemanticVersion;
            var increment = IncrementStrategyFinder.DetermineIncrementedField(context, baseVersion);
            if (increment != null)
            {
                semver = semver.IncrementVersion(increment.Value);
            }
            else Logger.WriteInfo("Skipping version increment");
            return semver;
        }

        SemanticVersion FindMainlineModeVersion(BaseVersion baseVersion, GitVersionContext context)
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
                var commitLog = context.Repository.Commits.QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = context.CurrentBranch,
                    ExcludeReachableFrom = baseVersion.BaseVersionSource,
                    SortBy = CommitSortStrategies.Reverse,
                    FirstParentOnly = true
                }).Where(c => c.Sha != baseVersion.BaseVersionSource.Sha).ToList();

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
                    var mainlineTip = GetMainlineTip(context);
                    var findMergeBase = context.Repository.ObjectDatabase.FindMergeBase(context.CurrentCommit, mainlineTip);
                    Logger.WriteInfo(string.Format("Current branch ({0}) was branch from {1}", context.CurrentBranch.FriendlyName, findMergeBase));

                    var branchIncrement = FindMessageIncrement(context, null, mergedHead, findMergeBase, directCommits);
                    // This will increment for any direct commits on master
                    mainlineVersion = IncrementForEachCommit(context, directCommits, mainlineVersion);
                    mainlineVersion.BuildMetaData = metaDataCalculator.Create(findMergeBase, context);
                    // Only increment if head is not a merge commit, ensures PR's and forward merges end up correct.
                    if (mergedHead.Parents.Count() == 1)
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

        private static SemanticVersion IncrementForEachCommit(GitVersionContext context, List<Commit> directCommits, SemanticVersion mainlineVersion)
        {
            foreach (var directCommit in directCommits)
            {
                var directCommitIncrement = IncrementStrategyFinder.GetIncrementForCommits(context, new[]
                                            {
                                                directCommit
                                            }) ?? VersionField.Patch;
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
            return IncrementStrategyFinder.GetIncrementForCommits(context, commits) ?? VersionField.Patch;
        }

        private Commit GetMergedHead(Commit mergeCommit)
        {
            var parents = mergeCommit.Parents.Skip(1).ToList();
            if (parents.Count > 1)
                throw new NotSupportedException("Mainline development does not support more than one merge source in a single commit yet");
            return parents.Single();
        }

        void UpdatePreReleaseTag(GitVersionContext context, SemanticVersion semanticVersion, string branchNameOverride)
        {
            var tagToUse = GetBranchSpecificTag(context.Configuration, context.CurrentBranch.FriendlyName, branchNameOverride);

            int? number = null;

            var lastTag = context.CurrentBranch
                .GetVersionTagsOnBranch(context.Repository, context.Configuration.GitTagPrefix)
                .FirstOrDefault(v => v.PreReleaseTag.Name == tagToUse);

            if (lastTag != null &&
                MajorMinorPatchEqual(lastTag, semanticVersion) &&
                lastTag.PreReleaseTag.HasTag())
            {
                number = lastTag.PreReleaseTag.Number + 1;
            }

            if (number == null)
            {
                number = 1;
            }

            semanticVersion.PreReleaseTag = new SemanticVersionPreReleaseTag(tagToUse, number);
        }

        public static string GetBranchSpecificTag(EffectiveConfiguration configuration, string branchFriendlyName, string branchNameOverride)
        {
            var tagToUse = configuration.Tag;
            if (tagToUse == "useBranchName")
            {
                tagToUse = "{BranchName}";
            }
            if (tagToUse.Contains("{BranchName}"))
            {
                Logger.WriteInfo("Using branch name to calculate version tag");

                var branchName = branchNameOverride ?? branchFriendlyName;
                if (!string.IsNullOrWhiteSpace(configuration.BranchPrefixToTrim))
                {
                    branchName = branchName.RegexReplace(configuration.BranchPrefixToTrim, string.Empty, RegexOptions.IgnoreCase);
                }
                branchName = branchName.RegexReplace("[^a-zA-Z0-9-]", "-");

                tagToUse = tagToUse.Replace("{BranchName}", branchName);
            }
            return tagToUse;
        }

        static bool MajorMinorPatchEqual(SemanticVersion lastTag, SemanticVersion baseVersion)
        {
            return lastTag.Major == baseVersion.Major &&
                   lastTag.Minor == baseVersion.Minor &&
                   lastTag.Patch == baseVersion.Patch;
        }
    }
}