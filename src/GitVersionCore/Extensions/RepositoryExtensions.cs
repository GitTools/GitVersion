using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitVersion.Logging;
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

        public static void DumpGraph(this IRepository repository, Action<string> writer = null, int? maxCommits = null)
        {
            LibGitExtensions.DumpGraph(repository.Info.Path, writer, maxCommits);
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

        private static IEnumerable<DirectReference> GetRemoteTipsUsingUsernamePasswordCredentials(this IRepository repository, Remote remote, string username, string password)
        {
            return repository.Network.ListReferences(remote, (url, fromUrl, types) => new UsernamePasswordCredentials
            {
                Username = username,
                Password = password
            }).Select(r => r.ResolveToDirectReference());
        }

        private static IEnumerable<DirectReference> GetRemoteTipsForAnonymousUser(this IRepository repository, Remote remote)
        {
            return repository.Network.ListReferences(remote).Select(r => r.ResolveToDirectReference());
        }
    }
}
