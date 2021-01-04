using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitVersion.Helpers;
using GitVersion.Logging;
using LibGit2Sharp;

namespace GitVersion.Extensions
{
    public static class GitExtensions
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

            return otherBranchFriendlyName.IsEquivalentTo(branchFriendlyName);
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

        public static Commit PeeledTargetCommit(this Tag tag)
        {
            var target = tag.Target;

            while (target is TagAnnotation annotation)
            {
                target = annotation.Target;
            }

            return target is LibGit2Sharp.Commit commit ? (Commit)commit : null;
        }

        public static IEnumerable<Commit> CommitsPriorToThan(this Branch branch, DateTimeOffset olderThan)
        {
            return branch.Commits.SkipWhile(c => c.When() > olderThan);
        }

        public static bool IsDetachedHead(this Branch branch)
        {
            return branch.CanonicalName.Equals("(no branch)", StringComparison.OrdinalIgnoreCase);
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
                    CreateGitLogArgs(maxCommits),
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
            if (String.Equals(branchName, branchNameToCompareAgainst, StringComparison.OrdinalIgnoreCase))
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

        public static string CreateGitLogArgs(int? maxCommits)
        {
            return @"log --graph --format=""%h %cr %d"" --decorate --date=relative --all --remotes=*" + (maxCommits != null ? $" -n {maxCommits}" : null);
        }
        public static bool GetMatchingCommitBranch(this IGitRepository repository, Commit baseVersionSource, Branch branch, Commit firstMatchingCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = (LibGit2Sharp.Branch)branch,
                ExcludeReachableFrom = (LibGit2Sharp.Commit)baseVersionSource,
                FirstParentOnly = true,
            };
            var commitCollection = repository.Commits.QueryBy(filter);

            return commitCollection.Contains(firstMatchingCommit);
        }
        public static IEnumerable<Commit> GetCommitsReacheableFrom(this IGitRepository repository, Commit commit, Branch branch)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = (LibGit2Sharp.Branch)branch
            };
            var commitCollection = repository.Commits.QueryBy(filter);

            return commitCollection.Where(c => c.Sha == commit.Sha);
        }
        public static List<Commit> GetCommitsReacheableFromHead(this IGitRepository repository, Commit headCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = (LibGit2Sharp.Commit)headCommit,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
            };

            var commitCollection = repository.Commits.QueryBy(filter);

            return commitCollection.ToList();
        }
        public static Commit GetForwardMerge(this IGitRepository repository, Commit commitToFindCommonBase, Commit findMergeBase)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = (LibGit2Sharp.Commit)commitToFindCommonBase,
                ExcludeReachableFrom = (LibGit2Sharp.Commit)findMergeBase
            };
            var commitCollection = repository.Commits.QueryBy(filter);

            var forwardMerge = commitCollection
                .FirstOrDefault(c => c.Parents.Contains(findMergeBase));
            return forwardMerge;
        }
        public static IEnumerable<Commit> GetMergeBaseCommits(this IGitRepository repository, Commit mergeCommit, Commit mergedHead, Commit findMergeBase)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = (LibGit2Sharp.Commit)mergedHead,
                ExcludeReachableFrom = (LibGit2Sharp.Commit)findMergeBase
            };
            var commitCollection = repository.Commits.QueryBy(filter);

            var commits = mergeCommit != null
                ? new[]
                {
                    mergeCommit
                }.Union(commitCollection)
                : commitCollection;
            return commits.ToList();
        }
        public static Commit GetBaseVersionSource(this IGitRepository repository, Commit currentBranchTip)
        {
            try
            {
                var filter = new CommitFilter
                {
                    IncludeReachableFrom = (LibGit2Sharp.Commit)currentBranchTip
                };
                var commitCollection = repository.Commits.QueryBy(filter);

                var baseVersionSource = commitCollection.First(c => !c.Parents.Any());
                return baseVersionSource;
            }
            catch (NotFoundException exception)
            {
                throw new GitVersionException($"Cannot find commit {currentBranchTip.Sha}. Please ensure that the repository is an unshallow clone with `git fetch --unshallow`.", exception);
            }
        }
        public static List<Commit> GetMainlineCommitLog(this IGitRepository repository, Commit baseVersionSource, Commit mainlineTip)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = (LibGit2Sharp.Commit)mainlineTip,
                ExcludeReachableFrom = (LibGit2Sharp.Commit)baseVersionSource,
                SortBy = CommitSortStrategies.Reverse,
                FirstParentOnly = true
            };
            var commitCollection = repository.Commits.QueryBy(filter);

            var mainlineCommitLog = commitCollection.ToList();
            return mainlineCommitLog;
        }
        public static CommitCollection GetCommitLog(this IGitRepository repository, Commit baseVersionSource, Commit currentCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = (LibGit2Sharp.Commit)currentCommit,
                ExcludeReachableFrom = (LibGit2Sharp.Commit)baseVersionSource,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            var commitCollection = repository.Commits.QueryBy(filter);

            return commitCollection;
        }

        public static Remote EnsureOnlyOneRemoteIsDefined(this IGitRepository repo, ILog log)
        {
            var remotes = repo.Network.Remotes;
            var howMany = remotes.Count();

            if (howMany == 1)
            {
                var remote = remotes.Single();
                log.Info($"One remote found ({remote.Name} -> '{remote.Url}').");
                repo.AddMissingRefSpecs(log, remote);
                return remote;
            }

            var message = $"{howMany} remote(s) have been detected. When being run on a build server, the Git repository is expected to bear one (and no more than one) remote.";
            throw new WarningException(message);
        }

        public static void CreateFakeBranchPointingAtThePullRequestTip(this IGitRepository repo, ILog log, AuthenticationInfo authentication)
        {
            var remote = repo.Network.Remotes.Single();

            log.Info("Fetching remote refs to see if there is a pull request ref");
            var remoteTips = (string.IsNullOrWhiteSpace(authentication.Username) ?
                    repo.GetRemoteTipsForAnonymousUser(remote) :
                    repo.GetRemoteTipsUsingUsernamePasswordCredentials(remote, authentication.Username, authentication.Password))
                .Select(r => r.ResolveToDirectReference()).ToList();

            log.Info($"Remote Refs:{System.Environment.NewLine}" + string.Join(System.Environment.NewLine, remoteTips.Select(r => r.CanonicalName)));

            var headTipSha = repo.Head.Tip.Sha;

            var refs = remoteTips.Where(r => r.TargetIdentifier == headTipSha).ToList();

            if (refs.Count == 0)
            {
                var message = $"Couldn't find any remote tips from remote '{remote.Url}' pointing at the commit '{headTipSha}'.";
                throw new WarningException(message);
            }

            if (refs.Count > 1)
            {
                var names = string.Join(", ", refs.Select(r => r.CanonicalName));
                var message = $"Found more than one remote tip from remote '{remote.Url}' pointing at the commit '{headTipSha}'. Unable to determine which one to use ({names}).";
                throw new WarningException(message);
            }

            var reference = refs[0];
            var canonicalName = reference.CanonicalName;
            log.Info($"Found remote tip '{canonicalName}' pointing at the commit '{headTipSha}'.");

            if (canonicalName.StartsWith("refs/tags"))
            {
                log.Info($"Checking out tag '{canonicalName}'");
                repo.Commands.Checkout(reference.Target.Sha);
                return;
            }

            if (!canonicalName.StartsWith("refs/pull/") && !canonicalName.StartsWith("refs/pull-requests/"))
            {
                var message = $"Remote tip '{canonicalName}' from remote '{remote.Url}' doesn't look like a valid pull request.";
                throw new WarningException(message);
            }

            var fakeBranchName = canonicalName.Replace("refs/pull/", "refs/heads/pull/").Replace("refs/pull-requests/", "refs/heads/pull-requests/");

            log.Info($"Creating fake local branch '{fakeBranchName}'.");
            repo.Refs.Add(fakeBranchName, new ObjectId(headTipSha));

            log.Info($"Checking local branch '{fakeBranchName}' out.");
            repo.Commands.Checkout(fakeBranchName);
        }

        private static IEnumerable<Reference> GetRemoteTipsUsingUsernamePasswordCredentials(this IGitRepository repository, Remote remote, string username, string password)
        {
            return repository.Network.ListReferences(remote, (url, fromUrl, types) => new UsernamePasswordCredentials
            {
                Username = username,
                Password = password ?? string.Empty
            });
        }

        private static IEnumerable<Reference> GetRemoteTipsForAnonymousUser(this IGitRepository repository, Remote remote)
        {
            return repository.Network.ListReferences(remote);
        }

        private static void AddMissingRefSpecs(this IGitRepository repo, ILog log, Remote remote)
        {
            if (remote.FetchRefSpecs.Any(r => r.Source == "refs/heads/*"))
                return;

            var allBranchesFetchRefSpec = $"+refs/heads/*:refs/remotes/{remote.Name}/*";

            log.Info($"Adding refspec: {allBranchesFetchRefSpec}");

            repo.Network.Remotes.Update(remote.Name, r => r.FetchRefSpecs.Add(allBranchesFetchRefSpec));
        }
    }
}
