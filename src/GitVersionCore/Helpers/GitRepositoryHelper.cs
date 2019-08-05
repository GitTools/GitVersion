namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    public static class GitRepositoryHelper
    {
        /// <summary>
        /// Normalisation of a git directory turns all remote branches into local branches, turns pull request refs into a real branch and a few other things. This is designed to be run *only on the build server* which checks out repositories in different ways.
        /// It is not recommended to run normalisation against a local repository
        /// </summary>
        public static void NormalizeGitDirectory(string gitDirectory, AuthenticationInfo authentication, bool noFetch, string currentBranch)
        {
            using (var repo = new Repository(gitDirectory))
            {
                // Need to ensure the HEAD does not move, this is essentially a BugCheck
                var expectedSha = repo.Head.Tip.Sha;
                var expectedBranchName = repo.Head.CanonicalName;

                try
                {
                    var remote = EnsureOnlyOneRemoteIsDefined(repo);

                    AddMissingRefSpecs(repo, remote);

                    //If noFetch is enabled, then GitVersion will assume that the git repository is normalized before execution, so that fetching from remotes is not required.
                    if (noFetch)
                    {
                        Logger.WriteInfo("Skipping fetching, if GitVersion does not calculate your version as expected you might need to allow fetching or use dynamic repositories");
                    }
                    else
                    {
                        Fetch(authentication, remote, repo);
                    }

                    EnsureLocalBranchExistsForCurrentBranch(repo, remote, currentBranch);
                    CreateOrUpdateLocalBranchesFromRemoteTrackingOnes(repo, remote.Name);

                    // Bug fix for https://github.com/GitTools/GitVersion/issues/1754, head maybe have been changed
                    // if this is a dynamic repository. But only allow this in case the branches are different (branch switch)
                    if (expectedSha != repo.Head.Tip.Sha &&
                        !expectedBranchName.IsBranch(currentBranch))
                    {
                        var newExpectedSha = repo.Head.Tip.Sha;
                        var newExpectedBranchName = repo.Head.CanonicalName;

                        Logger.WriteInfo($"Head has moved from '{expectedBranchName} | {expectedSha}' => '{newExpectedBranchName} | {newExpectedSha}', allowed since this is a dynamic repository");

                        expectedSha = newExpectedSha;
                        expectedBranchName = newExpectedBranchName;
                    }

                    var headSha = repo.Refs.Head.TargetIdentifier;

                    if (!repo.Info.IsHeadDetached)
                    {
                        Logger.WriteInfo($"HEAD points at branch '{headSha}'.");
                        return;
                    }

                    Logger.WriteInfo($"HEAD is detached and points at commit '{headSha}'.");
                    Logger.WriteInfo(string.Format("Local Refs:\r\n" + string.Join(Environment.NewLine, repo.Refs.FromGlob("*").Select(r => $"{r.CanonicalName} ({r.TargetIdentifier})"))));

                    // In order to decide whether a fake branch is required or not, first check to see if any local branches have the same commit SHA of the head SHA.
                    // If they do, go ahead and checkout that branch
                    // If no, go ahead and check out a new branch, using the known commit SHA as the pointer
                    var localBranchesWhereCommitShaIsHead = repo.Branches.Where(b => !b.IsRemote && b.Tip.Sha == headSha).ToList();

                    var matchingCurrentBranch = !string.IsNullOrEmpty(currentBranch)
                        ? localBranchesWhereCommitShaIsHead.SingleOrDefault(b => b.CanonicalName.Replace("/heads/", "/") == currentBranch.Replace("/heads/", "/"))
                        : null;
                    if (matchingCurrentBranch != null)
                    {
                        Logger.WriteInfo($"Checking out local branch '{currentBranch}'.");
                        Commands.Checkout(repo, matchingCurrentBranch);
                    }
                    else if (localBranchesWhereCommitShaIsHead.Count > 1)
                    {
                        var branchNames = localBranchesWhereCommitShaIsHead.Select(r => r.CanonicalName);
                        var csvNames = string.Join(", ", branchNames);
                        const string moveBranchMsg = "Move one of the branches along a commit to remove warning";

                        Logger.WriteWarning($"Found more than one local branch pointing at the commit '{headSha}' ({csvNames}).");
                        var master = localBranchesWhereCommitShaIsHead.SingleOrDefault(n => n.FriendlyName == "master");
                        if (master != null)
                        {
                            Logger.WriteWarning("Because one of the branches is 'master', will build master." + moveBranchMsg);
                            Commands.Checkout(repo, master);
                        }
                        else
                        {
                            var branchesWithoutSeparators = localBranchesWhereCommitShaIsHead.Where(b => !b.FriendlyName.Contains('/') && !b.FriendlyName.Contains('-')).ToList();
                            if (branchesWithoutSeparators.Count == 1)
                            {
                                var branchWithoutSeparator = branchesWithoutSeparators[0];
                                Logger.WriteWarning($"Choosing {branchWithoutSeparator.CanonicalName} as it is the only branch without / or - in it. " + moveBranchMsg);
                                Commands.Checkout(repo, branchWithoutSeparator);
                            }
                            else
                            {
                                throw new WarningException("Failed to try and guess branch to use. " + moveBranchMsg);
                            }
                        }
                    }
                    else if (localBranchesWhereCommitShaIsHead.Count == 0)
                    {
                        Logger.WriteInfo($"No local branch pointing at the commit '{headSha}'. Fake branch needs to be created.");
                        CreateFakeBranchPointingAtThePullRequestTip(repo, authentication);
                    }
                    else
                    {
                        Logger.WriteInfo($"Checking out local branch 'refs/heads/{localBranchesWhereCommitShaIsHead[0].FriendlyName}'.");
                        Commands.Checkout(repo, repo.Branches[localBranchesWhereCommitShaIsHead[0].FriendlyName]);
                    }
                }
                finally
                {
                    if (repo.Head.Tip.Sha != expectedSha)
                    {
                        if (Environment.GetEnvironmentVariable("IGNORE_NORMALISATION_GIT_HEAD_MOVE") != "1")
                        {
                            // Whoa, HEAD has moved, it shouldn't have. We need to blow up because there is a bug in normalisation
                            throw new BugException($@"GitVersion has a bug, your HEAD has moved after repo normalisation.

To disable this error set an environmental variable called IGNORE_NORMALISATION_GIT_HEAD_MOVE to 1

Please run `git {CreateGitLogArgs(100)}` and submit it along with your build log (with personal info removed) in a new issue at https://github.com/GitTools/GitVersion");
                        }
                    }
                }
            }
        }

        public static string CreateGitLogArgs(int? maxCommits)
        {
            return @"log --graph --format=""%h %cr %d"" --decorate --date=relative --all --remotes=*" + (maxCommits != null ? $" -n {maxCommits}" : null);
        }

        public static void Fetch(AuthenticationInfo authentication, Remote remote, Repository repo)
        {
            Logger.WriteInfo($"Fetching from remote '{remote.Name}' using the following refspecs: {string.Join(", ", remote.FetchRefSpecs.Select(r => r.Specification))}.");
            Commands.Fetch(repo, remote.Name, new string[0], authentication.ToFetchOptions(), null);
        }

        static void EnsureLocalBranchExistsForCurrentBranch(Repository repo, Remote remote, string currentBranch)
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
                Logger.WriteInfo(isBranch ? $"Creating local branch {localCanonicalName}"
                    : $"Creating local branch {localCanonicalName} pointing at {repoTipId}");
                repo.Refs.Add(localCanonicalName, repoTipId);
            }
            else
            {
                Logger.WriteInfo(isBranch ? $"Updating local branch {localCanonicalName} to point at {repoTip.Sha}"
                    : $"Updating local branch {localCanonicalName} to match ref {currentBranch}");
                repo.Refs.UpdateTarget(repo.Refs[localCanonicalName], repoTipId);
            }

            Commands.Checkout(repo, localCanonicalName);
        }

        static void AddMissingRefSpecs(Repository repo, Remote remote)
        {
            if (remote.FetchRefSpecs.Any(r => r.Source == "refs/heads/*"))
                return;

            var allBranchesFetchRefSpec = $"+refs/heads/*:refs/remotes/{remote.Name}/*";

            Logger.WriteInfo($"Adding refspec: {allBranchesFetchRefSpec}");

            repo.Network.Remotes.Update(remote.Name,
                r => r.FetchRefSpecs.Add(allBranchesFetchRefSpec));
        }

        static void CreateFakeBranchPointingAtThePullRequestTip(Repository repo, AuthenticationInfo authentication)
        {
            var remote = repo.Network.Remotes.Single();

            Logger.WriteInfo("Fetching remote refs to see if there is a pull request ref");
            var remoteTips = (string.IsNullOrEmpty(authentication.Username) ?
                    GetRemoteTipsForAnonymousUser(repo, remote) :
                    GetRemoteTipsUsingUsernamePasswordCredentials(repo, remote, authentication.Username, authentication.Password))
                .ToList();

            Logger.WriteInfo("Remote Refs:\r\n" + string.Join(Environment.NewLine, remoteTips.Select(r => r.CanonicalName)));

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
            Logger.WriteInfo($"Found remote tip '{canonicalName}' pointing at the commit '{headTipSha}'.");

            if (canonicalName.StartsWith("refs/tags"))
            {
                Logger.WriteInfo($"Checking out tag '{canonicalName}'");
                Commands.Checkout(repo, reference.Target.Sha);
                return;
            }

            if (!canonicalName.StartsWith("refs/pull/") && !canonicalName.StartsWith("refs/pull-requests/"))
            {
                var message = $"Remote tip '{canonicalName}' from remote '{remote.Url}' doesn't look like a valid pull request.";
                throw new WarningException(message);
            }

            var fakeBranchName = canonicalName.Replace("refs/pull/", "refs/heads/pull/").Replace("refs/pull-requests/", "refs/heads/pull-requests/");

            Logger.WriteInfo($"Creating fake local branch '{fakeBranchName}'.");
            repo.Refs.Add(fakeBranchName, new ObjectId(headTipSha));

            Logger.WriteInfo($"Checking local branch '{fakeBranchName}' out.");
            Commands.Checkout(repo, fakeBranchName);
        }

        static IEnumerable<DirectReference> GetRemoteTipsUsingUsernamePasswordCredentials(Repository repo, Remote remote, string username, string password)
        {
            return repo.Network.ListReferences(remote, (url, fromUrl, types) => new UsernamePasswordCredentials
            {
                Username = username,
                Password = password
            }).Select(r => r.ResolveToDirectReference());
        }

        static IEnumerable<DirectReference> GetRemoteTipsForAnonymousUser(Repository repo, Remote remote)
        {
            return repo.Network.ListReferences(remote).Select(r => r.ResolveToDirectReference());
        }

        static void CreateOrUpdateLocalBranchesFromRemoteTrackingOnes(Repository repo, string remoteName)
        {
            var prefix = $"refs/remotes/{remoteName}/";
            var remoteHeadCanonicalName = $"{prefix}{"HEAD"}";

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
                        Logger.WriteInfo($"Skipping update of '{remoteTrackingReference.CanonicalName}' as it already matches the remote ref.");
                        continue;
                    }
                    var remoteRefTipId = remotedirectReference.Target.Id;
                    Logger.WriteInfo($"Updating local ref '{localRef.CanonicalName}' to point at {remoteRefTipId}.");
                    repo.Refs.UpdateTarget(localRef, remoteRefTipId);
                    continue;
                }

                Logger.WriteInfo($"Creating local branch from remote tracking '{remoteTrackingReference.CanonicalName}'.");
                repo.Refs.Add(localCanonicalName, new ObjectId(remoteTrackingReference.ResolveToDirectReference().TargetIdentifier), true);

                var branch = repo.Branches[branchName];
                repo.Branches.Update(branch, b => b.TrackedBranch = remoteTrackingReferenceName);
            }
        }

        public static Remote EnsureOnlyOneRemoteIsDefined(IRepository repo)
        {
            var remotes = repo.Network.Remotes;
            var howMany = remotes.Count();

            if (howMany == 1)
            {
                var remote = remotes.Single();
                Logger.WriteInfo($"One remote found ({remote.Name} -> '{remote.Url}').");
                return remote;
            }

            var message = $"{howMany} remote(s) have been detected. When being run on a build server, the Git repository is expected to bear one (and no more than one) remote.";
            throw new WarningException(message);
        }
    }
}
