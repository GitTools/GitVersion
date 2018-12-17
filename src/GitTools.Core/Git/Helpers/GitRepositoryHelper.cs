namespace GitTools.Git
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using LibGit2Sharp;
    using Logging;

    /// <summary>
    /// Static helper class for creating/normalising git repositories
    /// </summary>
    public static class GitRepositoryHelper
    {
        static readonly ILog Log = LogProvider.GetLogger(typeof(GitRepositoryHelper));

        /// <summary>
        /// Normalisation of a git directory turns all remote branches into local branches, turns pull request refs into a real branch and a few other things. This is designed to be run *only on the build server* which checks out repositories in different ways.
        /// It is not recommended to run normalisation against a local repository
        /// </summary>
        public static void NormalizeGitDirectory(string gitDirectory, AuthenticationInfo authentication, bool noFetch, string currentBranch)
        {
            using (var repo = new Repository(gitDirectory))
            {
                // Need to unsure the HEAD does not move, this is essentially a BugCheck
                var expectedSha = repo.Head.Tip.Sha;
                try
                {
                    var remote = EnsureOnlyOneRemoteIsDefined(repo);

                    AddMissingRefSpecs(repo, remote);

                    //If noFetch is enabled, then GitVersion will assume that the git repository is normalized before execution, so that fetching from remotes is not required.
                    if (noFetch)
                    {
                        Log.Info("Skipping fetching, if GitVersion does not calculate your version as expected you might need to allow fetching or use dynamic repositories");
                    }
                    else
                    {
                        Fetch(authentication, remote, repo);
                    }

                    EnsureLocalBranchExistsForCurrentBranch(repo, remote, currentBranch);
                    CreateOrUpdateLocalBranchesFromRemoteTrackingOnes(repo, remote.Name);

                    var headSha = repo.Refs.Head.TargetIdentifier;

                    if (!repo.Info.IsHeadDetached)
                    {
                        Log.Info(string.Format("HEAD points at branch '{0}'.", headSha));
                        return;
                    }

                    Log.Info(string.Format("HEAD is detached and points at commit '{0}'.", headSha));
                    Log.Info(string.Format("Local Refs:\r\n" + string.Join(Environment.NewLine, repo.Refs.FromGlob("*").Select(r => string.Format("{0} ({1})", r.CanonicalName, r.TargetIdentifier)))));

                    // In order to decide whether a fake branch is required or not, first check to see if any local branches have the same commit SHA of the head SHA.
                    // If they do, go ahead and checkout that branch
                    // If no, go ahead and check out a new branch, using the known commit SHA as the pointer
                    var localBranchesWhereCommitShaIsHead = repo.Branches.Where(b => !b.IsRemote && b.Tip.Sha == headSha).ToList();

                    var matchingCurrentBranch = !string.IsNullOrEmpty(currentBranch)
                        ? localBranchesWhereCommitShaIsHead.SingleOrDefault(b => b.CanonicalName.Replace("/heads/", "/") == currentBranch.Replace("/heads/", "/"))
                        : null;
                    if (matchingCurrentBranch != null)
                    {
                        Log.Info(string.Format("Checking out local branch '{0}'.", currentBranch));
                        Commands.Checkout(repo, matchingCurrentBranch);
                    }
                    else if (localBranchesWhereCommitShaIsHead.Count > 1)
                    {
                        var branchNames = localBranchesWhereCommitShaIsHead.Select(r => r.CanonicalName);
                        var csvNames = string.Join(", ", branchNames);
                        const string moveBranchMsg = "Move one of the branches along a commit to remove warning";

                        Log.Warn(string.Format("Found more than one local branch pointing at the commit '{0}' ({1}).", headSha, csvNames));
                        var master = localBranchesWhereCommitShaIsHead.SingleOrDefault(n => n.FriendlyName == "master");
                        if (master != null)
                        {
                            Log.Warn("Because one of the branches is 'master', will build master." + moveBranchMsg);
                            Commands.Checkout(repo, master);
                        }
                        else
                        {
                            var branchesWithoutSeparators = localBranchesWhereCommitShaIsHead.Where(b => !b.FriendlyName.Contains('/') && !b.FriendlyName.Contains('-')).ToList();
                            if (branchesWithoutSeparators.Count == 1)
                            {
                                var branchWithoutSeparator = branchesWithoutSeparators[0];
                                Log.Warn(string.Format("Choosing {0} as it is the only branch without / or - in it. " + moveBranchMsg, branchWithoutSeparator.CanonicalName));
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
                        Log.Info(string.Format("No local branch pointing at the commit '{0}'. Fake branch needs to be created.", headSha));
                        CreateFakeBranchPointingAtThePullRequestTip(repo, authentication);
                    }
                    else
                    {
                        Log.Info(string.Format("Checking out local branch 'refs/heads/{0}'.", localBranchesWhereCommitShaIsHead[0].FriendlyName));
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
                            throw new BugException(string.Format(@"GitTools.Core has a bug, your HEAD has moved after repo normalisation.

To disable this error set an environmental variable called IGNORE_NORMALISATION_GIT_HEAD_MOVE to 1

Please run `git {0}` and submit it along with your build log (with personal info removed) in a new issue at https://github.com/GitTools/GitTools.Core",
                                LibGitExtensions.CreateGitLogArgs(100)));
                        }
                    }
                }
            }
        }

        public static void Fetch(AuthenticationInfo authentication, Remote remote, Repository repo)
        {
            Log.Info(string.Format("Fetching from remote '{0}' using the following refspecs: {1}.",
                remote.Name, string.Join(", ", remote.FetchRefSpecs.Select(r => r.Specification))));
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
            var originCanonicalName = string.Format("{0}/{1}", remote.Name, currentBranch);
            var originBranch = repo.Branches[originCanonicalName];
            if (originBranch != null)
            {
                repoTip = originBranch.Tip;
            }

            var repoTipId = repoTip.Id;

            if (repo.Branches.All(b => b.CanonicalName != localCanonicalName))
            {
                Log.Info(isBranch ?
                    string.Format("Creating local branch {0}", localCanonicalName) :
                    string.Format("Creating local branch {0} pointing at {1}", localCanonicalName, repoTipId));
                repo.Refs.Add(localCanonicalName, repoTipId);
            }
            else
            {
                Log.Info(isBranch ?
                    string.Format("Updating local branch {0} to point at {1}", localCanonicalName, repoTip.Sha) :
                    string.Format("Updating local branch {0} to match ref {1}", localCanonicalName, currentBranch));
                repo.Refs.UpdateTarget(repo.Refs[localCanonicalName], repoTipId);
            }

            Commands.Checkout(repo, localCanonicalName);
        }

        static void AddMissingRefSpecs(Repository repo, Remote remote)
        {
            if (remote.FetchRefSpecs.Any(r => r.Source == "refs/heads/*"))
                return;

            var allBranchesFetchRefSpec = string.Format("+refs/heads/*:refs/remotes/{0}/*", remote.Name);

            Log.Info(string.Format("Adding refspec: {0}", allBranchesFetchRefSpec));

            repo.Network.Remotes.Update(remote.Name,
                r => r.FetchRefSpecs.Add(allBranchesFetchRefSpec));
        }

        static void CreateFakeBranchPointingAtThePullRequestTip(Repository repo, AuthenticationInfo authentication)
        {
            var remote = repo.Network.Remotes.Single();

            Log.Info("Fetching remote refs to see if there is a pull request ref");
            var remoteTips = (string.IsNullOrEmpty(authentication.Username) ?
                GetRemoteTipsForAnonymousUser(repo, remote) :
                GetRemoteTipsUsingUsernamePasswordCredentials(repo, remote, authentication.Username, authentication.Password))
                .ToList();

            Log.Info("Remote Refs:\r\n" + string.Join(Environment.NewLine, remoteTips.Select(r => r.CanonicalName)));

            var headTipSha = repo.Head.Tip.Sha;

            var refs = remoteTips.Where(r => r.TargetIdentifier == headTipSha).ToList();

            if (refs.Count == 0)
            {
                var message = string.Format("Couldn't find any remote tips from remote '{0}' pointing at the commit '{1}'.", remote.Url, headTipSha);
                throw new WarningException(message);
            }

            if (refs.Count > 1)
            {
                var names = string.Join(", ", refs.Select(r => r.CanonicalName));
                var message = string.Format("Found more than one remote tip from remote '{0}' pointing at the commit '{1}'. Unable to determine which one to use ({2}).", remote.Url, headTipSha, names);
                throw new WarningException(message);
            }

            var reference = refs[0];
            var canonicalName = reference.CanonicalName;
            Log.Info(string.Format("Found remote tip '{0}' pointing at the commit '{1}'.", canonicalName, headTipSha));

            if (canonicalName.StartsWith("refs/tags"))
            {
                Log.Info(string.Format("Checking out tag '{0}'", canonicalName));
                Commands.Checkout(repo, reference.Target.Sha);
                return;
            }

            if (!canonicalName.StartsWith("refs/pull/") && !canonicalName.StartsWith("refs/pull-requests/"))
            {
                var message = string.Format("Remote tip '{0}' from remote '{1}' doesn't look like a valid pull request.", canonicalName, remote.Url);
                throw new WarningException(message);
            }

            var fakeBranchName = canonicalName.Replace("refs/pull/", "refs/heads/pull/").Replace("refs/pull-requests/", "refs/heads/pull-requests/");

            Log.Info(string.Format("Creating fake local branch '{0}'.", fakeBranchName));
            repo.Refs.Add(fakeBranchName, new ObjectId(headTipSha));

            Log.Info(string.Format("Checking local branch '{0}' out.", fakeBranchName));
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
            var prefix = string.Format("refs/remotes/{0}/", remoteName);
            var remoteHeadCanonicalName = string.Format("{0}{1}", prefix, "HEAD");

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
                        Log.Info(string.Format("Skipping update of '{0}' as it already matches the remote ref.", remoteTrackingReference.CanonicalName));
                        continue;
                    }
                    var remoteRefTipId = remotedirectReference.Target.Id;
                    Log.Info(string.Format("Updating local ref '{0}' to point at {1}.", localRef.CanonicalName, remoteRefTipId));
                    repo.Refs.UpdateTarget(localRef, remoteRefTipId);
                    continue;
                }

                Log.Info(string.Format("Creating local branch from remote tracking '{0}'.", remoteTrackingReference.CanonicalName));
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
                Log.Info(string.Format("One remote found ({0} -> '{1}').", remote.Name, remote.Url));
                return remote;
            }

            var message = string.Format("{0} remote(s) have been detected. When being run on a build server, the Git repository is expected to bear one (and no more than one) remote.", howMany);
            throw new WarningException(message);
        }
    }
}