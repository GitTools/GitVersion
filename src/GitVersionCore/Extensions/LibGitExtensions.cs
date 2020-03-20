using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitVersion.Configuration;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Extensions
{
    public static class LibGitExtensions
    {
        public static DateTimeOffset When(this Commit commit)
        {
            return commit.Committer.When;
        }

        public static string NameWithoutRemote(this Branch branch)
        {
            return branch.IsRemote
                ? branch.FriendlyName.Substring(branch.FriendlyName.IndexOf("/", StringComparison.Ordinal) + 1)
                : branch.FriendlyName;
        }

        public static string NameWithoutOrigin(this Branch branch)
        {
            return branch.IsRemote && branch.FriendlyName.StartsWith("origin/")
                ? branch.FriendlyName.Substring("origin/".Length)
                : branch.FriendlyName;
        }

        /// <summary>
        /// Checks if the two branch objects refer to the same branch (have the same friendly name).
        /// </summary>
        public static bool IsSameBranch(this Branch branch, Branch otherBranch)
        {
            // For each branch, fixup the friendly name if the branch is remote.
            var otherBranchFriendlyName = otherBranch.NameWithoutRemote();
            var branchFriendlyName = branch.NameWithoutRemote();

            return otherBranchFriendlyName == branchFriendlyName;
        }

        /// <summary>
        /// Exclude the given branches (by value equality according to friendly name).
        /// </summary>
        public static IEnumerable<BranchCommit> ExcludingBranches(this IEnumerable<BranchCommit> branches, IEnumerable<Branch> branchesToExclude)
        {
            return branches.Where(b => branchesToExclude.All(bte => !IsSameBranch(b.Branch, bte)));
        }

        /// <summary>
        /// Exclude the given branches (by value equality according to friendly name).
        /// </summary>
        public static IEnumerable<Branch> ExcludingBranches(this IEnumerable<Branch> branches, IEnumerable<Branch> branchesToExclude)
        {
            return branches.Where(b => branchesToExclude.All(bte => !IsSameBranch(b, bte)));
        }

        public static GitObject PeeledTarget(this Tag tag)
        {
            var target = tag.Target;

            while (target is TagAnnotation annotation)
            {
                target = annotation.Target;
            }
            return target;
        }

        public static IEnumerable<Commit> CommitsPriorToThan(this Branch branch, DateTimeOffset olderThan)
        {
            return branch.Commits.SkipWhile(c => c.When() > olderThan);
        }

        public static bool IsDetachedHead(this Branch branch)
        {
            return branch.CanonicalName.Equals("(no branch)", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetRepositoryDirectory(this IRepository repository, bool omitGitPostFix = true)
        {
            var gitDirectory = repository.Info.Path;

            gitDirectory = gitDirectory.TrimEnd(Path.DirectorySeparatorChar);

            if (omitGitPostFix && gitDirectory.EndsWith(".git"))
            {
                gitDirectory = gitDirectory.Substring(0, gitDirectory.Length - ".git".Length);
                gitDirectory = gitDirectory.TrimEnd(Path.DirectorySeparatorChar);
            }

            return gitDirectory;
        }

        public static Branch FindBranch(this IRepository repository, string branchName)
        {
            return repository.Branches.FirstOrDefault(x => x.NameWithoutRemote() == branchName);
        }

        public static Commit GetBaseVersionSource(this IRepository repository, Commit currentBranchTip)
        {
            var baseVersionSource = repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = currentBranchTip
            }).First(c => !c.Parents.Any());
            return baseVersionSource;
        }

        public static List<Commit> GetCommitsReacheableFromHead(this IRepository repository, Commit headCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = headCommit,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
            };

            var commitCache = repository.Commits.QueryBy(filter).ToList();
            return commitCache;
        }

        public static Commit GetForwardMerge(this IRepository repository, Commit commitToFindCommonBase, Commit findMergeBase)
        {
            var forwardMerge = repository.Commits
                .QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = commitToFindCommonBase,
                    ExcludeReachableFrom = findMergeBase
                })
                .FirstOrDefault(c => c.Parents.Contains(findMergeBase));
            return forwardMerge;
        }

        public static IEnumerable<Commit> GetCommitsReacheableFrom(this IRepository repository, Commit commit, Branch branch)
        {
            var commits = repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = branch
            }).Where(c => c.Sha == commit.Sha);
            return commits;
        }

        public static ICommitLog GetCommitLog(this IRepository repository, Commit baseVersionSource, Commit currentCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = currentCommit,
                ExcludeReachableFrom = baseVersionSource,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            var commitLog = repository.Commits.QueryBy(filter);
            return commitLog;
        }

        public static List<Commit> GetMainlineCommitLog(this IRepository repository, BaseVersion baseVersion, Commit mainlineTip)
        {
            var mainlineCommitLog = repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = mainlineTip,
                ExcludeReachableFrom = baseVersion.BaseVersionSource,
                SortBy = CommitSortStrategies.Reverse,
                FirstParentOnly = true
            })
                .ToList();
            return mainlineCommitLog;
        }

        public static List<Commit> GetMainlineCommitLog(this IRepository repository, Commit baseVersionSource, Branch mainline)
        {
            var mainlineCommitLog = repository.Commits
                .QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = mainline.Tip,
                    ExcludeReachableFrom = baseVersionSource,
                    SortBy = CommitSortStrategies.Reverse,
                    FirstParentOnly = true
                })
                .ToList();
            return mainlineCommitLog;
        }

        public static bool GetMatchingCommitBranch(this IRepository repository, Commit baseVersionSource, Branch branch, Commit firstMatchingCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = branch,
                ExcludeReachableFrom = baseVersionSource,
                FirstParentOnly = true,
            };
            var query = repository.Commits.QueryBy(filter);

            return query.Contains(firstMatchingCommit);
        }
        public static List<Commit> GetMergeBaseCommits(this IRepository repository, Commit mergeCommit, Commit mergedHead, Commit findMergeBase)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = mergedHead,
                ExcludeReachableFrom = findMergeBase
            };
            var query = repository.Commits.QueryBy(filter);

            var commits = mergeCommit == null ? query.ToList() : new[] { mergeCommit }.Union(query).ToList();
            return commits;
        }

        public static void DumpGraph(this IRepository repository, Action<string> writer = null, int? maxCommits = null)
        {
            DumpGraph(repository.Info.Path, writer, maxCommits);
        }

        public static void DumpGraph(string workingDirectory, Action<string> writer = null, int? maxCommits = null)
        {
            var output = new StringBuilder();
            try
            {
                ProcessHelper.Run(
                    o => output.AppendLine(o),
                    e => output.AppendLineFormat("ERROR: {0}", e),
                    null,
                    "git",
                    GitRepositoryHelper.CreateGitLogArgs(maxCommits),
                    workingDirectory);
            }
            catch (FileNotFoundException exception)
            {
                if (exception.FileName != "git")
                {
                    throw;
                }

                output.AppendLine("Could not execute 'git log' due to the following error:");
                output.AppendLine(exception.ToString());
            }

            if (writer != null)
            {
                writer(output.ToString());
            }
            else
            {
                Console.Write(output.ToString());
            }
        }

        public static bool IsBranch(this string branchName, string branchNameToCompareAgainst)
        {
            // "develop" == "develop"
            if (string.Equals(branchName, branchNameToCompareAgainst, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // "refs/head/develop" == "develop"
            if (branchName.EndsWith($"/{branchNameToCompareAgainst}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static Branch GetTargetBranch(this IRepository repository, string targetBranch)
        {
            // By default, we assume HEAD is pointing to the desired branch
            var desiredBranch = repository.Head;

            // Make sure the desired branch has been specified
            if (!string.IsNullOrEmpty(targetBranch))
            {
                // There are some edge cases where HEAD is not pointing to the desired branch.
                // Therefore it's important to verify if 'currentBranch' is indeed the desired branch.

                // CanonicalName can be "refs/heads/develop", so we need to check for "/{TargetBranch}" as well
                if (!desiredBranch.CanonicalName.IsBranch(targetBranch))
                {
                    // In the case where HEAD is not the desired branch, try to find the branch with matching name
                    desiredBranch = repository?.Branches?
                        .SingleOrDefault(b =>
                            b.CanonicalName == targetBranch ||
                            b.FriendlyName == targetBranch ||
                            b.NameWithoutRemote() == targetBranch);

                    // Failsafe in case the specified branch is invalid
                    desiredBranch ??= repository.Head;
                }
            }

            return desiredBranch;
        }

        public static Commit GetCurrentCommit(this IRepository repository, ILog log, Branch currentBranch, string commitId)
        {
            Commit currentCommit = null;
            if (!string.IsNullOrWhiteSpace(commitId))
            {
                log.Info($"Searching for specific commit '{commitId}'");

                var commit = repository.Commits.FirstOrDefault(c => string.Equals(c.Sha, commitId, StringComparison.OrdinalIgnoreCase));
                if (commit != null)
                {
                    currentCommit = commit;
                }
                else
                {
                    log.Warning($"Commit '{commitId}' specified but not found");
                }
            }

            if (currentCommit == null)
            {
                log.Info("Using latest commit on specified branch");
                currentCommit = currentBranch.Tip;
            }

            return currentCommit;

        }

        public static SemanticVersion GetCurrentCommitTaggedVersion(this IRepository repository, GitObject commit, EffectiveConfiguration config)
        {
            return repository.Tags
                .SelectMany(t =>
                {
                    if (t.PeeledTarget() == commit && SemanticVersion.TryParse(t.FriendlyName, config.GitTagPrefix, out var version))
                        return new[] {
                            version
                        };
                    return new SemanticVersion[0];
                })
                .Max();
        }
    }
}
