using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Extensions
{
    public static class RepositoryExtensions
    {
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

            return repository.Commits.QueryBy(filter).ToList();
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

        public static IEnumerable<Commit> GetMergeBaseCommits(this IRepository repository, Commit mergeCommit, Commit mergedHead, Commit findMergeBase)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = mergedHead,
                ExcludeReachableFrom = findMergeBase
            };
            var query = repository.Commits.QueryBy(filter);

            var commits = mergeCommit == null ? query.ToList() : new[] {
                mergeCommit
            }.Union(query).ToList();
            return commits;
        }

        public static void DumpGraph(this IRepository repository, Action<string> writer = null, int? maxCommits = null)
        {
            LibGitExtensions.DumpGraph(repository.Info.Path, writer, maxCommits);
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
                    desiredBranch = repository.Branches?
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

        public static IEnumerable<DirectReference> GetRemoteTipsUsingUsernamePasswordCredentials(this IRepository repository, Remote remote, string username, string password)
        {
            return repository.Network.ListReferences(remote, (url, fromUrl, types) => new UsernamePasswordCredentials
            {
                Username = username,
                Password = password
            }).Select(r => r.ResolveToDirectReference());
        }

        public static IEnumerable<DirectReference> GetRemoteTipsForAnonymousUser(this IRepository repository, Remote remote)
        {
            return repository.Network.ListReferences(remote).Select(r => r.ResolveToDirectReference());
        }

        public static void EnsureLocalBranchExistsForCurrentBranch(this IRepository repo, ILog log, Remote remote, string currentBranch)
        {
            if (string.IsNullOrEmpty(currentBranch)) return;

            var isRef = currentBranch.Contains("refs");
            var isBranch = currentBranch.Contains("refs/heads");
            var localCanonicalName = !isRef ? "refs/heads/" + currentBranch : isBranch ? currentBranch : currentBranch.Replace("refs/", "refs/heads/");

            var repoTip = repo.Head.Tip;

            // We currently have the rep.Head of the *default* branch, now we need to look up the right one
            var originCanonicalName = $"{remote.Name}/{currentBranch}";
            var originBranch = repo.Branches[originCanonicalName];
            if (originBranch != null)
            {
                repoTip = originBranch.Tip;
            }

            var repoTipId = repoTip.Id;

            if (repo.Branches.All(b => b.CanonicalName != localCanonicalName))
            {
                log.Info(isBranch ? $"Creating local branch {localCanonicalName}"
                    : $"Creating local branch {localCanonicalName} pointing at {repoTipId}");
                repo.Refs.Add(localCanonicalName, repoTipId);
            }
            else
            {
                log.Info(isBranch ? $"Updating local branch {localCanonicalName} to point at {repoTip.Sha}"
                    : $"Updating local branch {localCanonicalName} to match ref {currentBranch}");
                repo.Refs.UpdateTarget(repo.Refs[localCanonicalName], repoTipId);
            }

            Commands.Checkout(repo, localCanonicalName);
        }

        public static void AddMissingRefSpecs(this IRepository repo, ILog log, Remote remote)
        {
            if (remote.FetchRefSpecs.Any(r => r.Source == "refs/heads/*"))
                return;

            var allBranchesFetchRefSpec = $"+refs/heads/*:refs/remotes/{remote.Name}/*";

            log.Info($"Adding refspec: {allBranchesFetchRefSpec}");

            repo.Network.Remotes.Update(remote.Name,
                r => r.FetchRefSpecs.Add(allBranchesFetchRefSpec));
        }

        public static void CreateFakeBranchPointingAtThePullRequestTip(this IRepository repo, ILog log, AuthenticationInfo authentication)
        {
            var remote = repo.Network.Remotes.Single();

            log.Info("Fetching remote refs to see if there is a pull request ref");
            var remoteTips = (string.IsNullOrEmpty(authentication.Username) ?
                    repo.GetRemoteTipsForAnonymousUser(remote) :
                    repo.GetRemoteTipsUsingUsernamePasswordCredentials(remote, authentication.Username, authentication.Password))
                .ToList();

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
                Commands.Checkout(repo, reference.Target.Sha);
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
            Commands.Checkout(repo, fakeBranchName);
        }

        public static void CreateOrUpdateLocalBranchesFromRemoteTrackingOnes(this IRepository repo, ILog log, string remoteName)
        {
            var prefix = $"refs/remotes/{remoteName}/";
            var remoteHeadCanonicalName = $"{prefix}HEAD";

            foreach (var remoteTrackingReference in repo.Refs.FromGlob(prefix + "*").Where(r => r.CanonicalName != remoteHeadCanonicalName))
            {
                var remoteTrackingReferenceName = remoteTrackingReference.CanonicalName;
                var branchName = remoteTrackingReferenceName.Substring(prefix.Length);
                var localCanonicalName = "refs/heads/" + branchName;

                // We do not want to touch our current branch
                if (branchName == repo.Head.FriendlyName) continue;

                if (repo.Refs.Any(x => x.CanonicalName == localCanonicalName))
                {
                    var localRef = repo.Refs[localCanonicalName];
                    var remotedirectReference = remoteTrackingReference.ResolveToDirectReference();
                    if (localRef.ResolveToDirectReference().TargetIdentifier == remotedirectReference.TargetIdentifier)
                    {
                        log.Info($"Skipping update of '{remoteTrackingReference.CanonicalName}' as it already matches the remote ref.");
                        continue;
                    }
                    var remoteRefTipId = remotedirectReference.Target.Id;
                    log.Info($"Updating local ref '{localRef.CanonicalName}' to point at {remoteRefTipId}.");
                    repo.Refs.UpdateTarget(localRef, remoteRefTipId);
                    continue;
                }

                log.Info($"Creating local branch from remote tracking '{remoteTrackingReference.CanonicalName}'.");
                repo.Refs.Add(localCanonicalName, new ObjectId(remoteTrackingReference.ResolveToDirectReference().TargetIdentifier), true);

                var branch = repo.Branches[branchName];
                repo.Branches.Update(branch, b => b.TrackedBranch = remoteTrackingReferenceName);
            }
        }

        public static Remote EnsureOnlyOneRemoteIsDefined(this IRepository repo, ILog log)
        {
            var remotes = repo.Network.Remotes;
            var howMany = remotes.Count();

            if (howMany == 1)
            {
                var remote = remotes.Single();
                log.Info($"One remote found ({remote.Name} -> '{remote.Url}').");
                return remote;
            }

            var message = $"{howMany} remote(s) have been detected. When being run on a build server, the Git repository is expected to bear one (and no more than one) remote.";
            throw new WarningException(message);
        }
    }
}
