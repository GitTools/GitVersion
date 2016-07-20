namespace GitVersion.VersionCalculation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BaseVersionCalculators;
    using GitTools;
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
                    new VersionInBranchBaseVersionStrategy(),
                    new DevelopVersionStrategy());
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
            var semver = context.Configuration.VersioningMode == VersioningMode.Mainline ?
                FindMainlineModeVersion(baseVersion, context) :
                PerformIncrement(context, baseVersion);

            if (!semver.PreReleaseTag.HasTag() && !string.IsNullOrEmpty(context.Configuration.Tag))
            {
                UpdatePreReleaseTag(context, semver, baseVersion.BranchNameOverride);
            }

            semver.BuildMetaData = metaDataCalculator.Create(baseVersion.BaseVersionSource, context);

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

        private SemanticVersion FindMainlineModeVersion(BaseVersion baseVersion, GitVersionContext context)
        {
            if (baseVersion.SemanticVersion.PreReleaseTag.HasTag())
            {
                throw new NotSupportedException("Mainline development mode doesn't yet support pre-release tags on master");
            }

            using (Logger.IndentLog("Using mainline development mode to calculate current version"))
            {
                var mainlineVersion = baseVersion.SemanticVersion;

                var commitLog = context.Repository.Commits.QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = context.CurrentBranch,
                    ExcludeReachableFrom = baseVersion.BaseVersionSource,
                    SortBy = CommitSortStrategies.Reverse
                }).Where(c => c.Sha != baseVersion.BaseVersionSource.Sha).ToList();
                var directCommits = new List<Commit>();

                foreach (var commit in commitLog)
                {
                    directCommits.Add(commit);
                    if (commit.Parents.Count() > 1)
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
                    }
                }

                if (context.CurrentBranch.FriendlyName != "master")
                {
                    var mergedHead = context.CurrentCommit;
                    var findMergeBase = context.Repository.ObjectDatabase.FindMergeBase(context.CurrentCommit, context.Repository.FindBranch("master").Tip);
                    Logger.WriteInfo(string.Format("Current branch ({0}) was branch from {1}", context.CurrentBranch.FriendlyName, findMergeBase));

                    var branchIncrement = FindMessageIncrement(context, findMergeBase, mergedHead, findMergeBase, directCommits);
                    mainlineVersion = IncrementForEachCommit(context, directCommits, mainlineVersion);
                    Logger.WriteInfo(string.Format("Performing {0} increment for current branch ", branchIncrement));
                    mainlineVersion = mainlineVersion.IncrementVersion(branchIncrement);
                }
                else
                {
                    // If we are on master, make sure no commits get left behind
                    mainlineVersion = IncrementForEachCommit(context, directCommits, mainlineVersion);
                }

                return mainlineVersion;
            }
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
            var commits = new[] { mergeCommit }.Union(context.Repository.Commits.QueryBy(filter)).ToList();
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
            if (!string.IsNullOrEmpty(context.Configuration.TagNumberPattern))
            {
                var match = Regex.Match(context.CurrentBranch.CanonicalName, context.Configuration.TagNumberPattern);
                var numberGroup = match.Groups["number"];
                if (numberGroup.Success)
                {
                    number = int.Parse(numberGroup.Value);
                }
            }

            var lastTag = context.CurrentBranch
                .GetVersionTagsOnBranch(context.Repository, context.Configuration.GitTagPrefix)
                .FirstOrDefault(v => v.PreReleaseTag.Name == tagToUse);

            if (number == null &&
                lastTag != null &&
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