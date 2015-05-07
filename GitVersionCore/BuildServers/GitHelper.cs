namespace GitVersion
{
    using LibGit2Sharp;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class GitHelper
    {
        const string MergeMessageRegexPattern = "refs/heads/(pr|pull(-requests)?/(?<issuenumber>[0-9]*)/(merge|head))";

        public static void NormalizeGitDirectory(string gitDirectory, Authentication authentication, bool noFetch)
        {
            //If noFetch is enabled, then GitVersion will assume that the git repository is normalized before execution, so that fetching from remotes is not required.
            if (noFetch)
            {
                Logger.WriteInfo("Skipping fetching");
                return;
            }

            using (var repo = new Repository(gitDirectory))
            {
                var remote = EnsureOnlyOneRemoteIsDefined(repo);

                AddMissingRefSpecs(repo, remote);

                Logger.WriteInfo(string.Format("Fetching from remote '{0}' using the following refspecs: {1}.",
                    remote.Name, string.Join(", ", remote.FetchRefSpecs.Select(r => r.Specification))));

                var fetchOptions = BuildFetchOptions(authentication.Username, authentication.Password);
                repo.Network.Fetch(remote, fetchOptions);

                CreateMissingLocalBranchesFromRemoteTrackingOnes(repo, remote.Name);

                var headSha = repo.Refs.Head.TargetIdentifier;

                if (!repo.Info.IsHeadDetached)
                {
                    Logger.WriteInfo(string.Format("HEAD points at branch '{0}'.", headSha));
                    return;
                }

                Logger.WriteInfo(string.Format("HEAD is detached and points at commit '{0}'.", headSha));

                // In order to decide whether a fake branch is required or not, first check to see if any local branches have the same commit SHA of the head SHA.
                // If they do, go ahead and checkout that branch
                // If no, go ahead and check out a new branch, using the known commit SHA as the pointer
                var localBranchesWhereCommitShaIsHead = repo.Branches.Where(b => !b.IsRemote && b.Tip.Sha == headSha).ToList();

                if (localBranchesWhereCommitShaIsHead.Count > 1)
                {
                    var names = string.Join(", ", localBranchesWhereCommitShaIsHead.Select(r => r.CanonicalName));
                    var message = string.Format("Found more than one local branch pointing at the commit '{0}'. Unable to determine which one to use ({1}).", headSha, names);
                    throw new WarningException(message);
                }

                if (localBranchesWhereCommitShaIsHead.Count == 0)
                {
                    Logger.WriteInfo(string.Format("No local branch pointing at the commit '{0}'. Fake branch needs to be created.", headSha));
                    CreateFakeBranchPointingAtThePullRequestTip(repo, authentication);
                }
                else
                {
                    Logger.WriteInfo(string.Format("Checking out local branch 'refs/heads/{0}'.", localBranchesWhereCommitShaIsHead[0].Name));
                    repo.Branches[localBranchesWhereCommitShaIsHead[0].Name].Checkout();
                }
            }
        }

        public static bool LooksLikeAValidPullRequestNumber(string issueNumber)
        {
            if (string.IsNullOrEmpty(issueNumber))
            {
                return false;
            }

            uint res;
            return uint.TryParse(issueNumber, out res);
        }

        public static string ExtractIssueNumber(string mergeMessage)
        {
            // Dynamic: refs/heads/pr/5
            // Github Message: refs/heads/pull/5/merge
            // Stash Message:  refs/heads/pull-requests/5/merge
            // refs/heads/pull/5/head
            var regex = new Regex(MergeMessageRegexPattern);
            var match = regex.Match(mergeMessage);

            var issueNumber = match.Groups["issuenumber"].Value;

            return issueNumber;
        }

        static void AddMissingRefSpecs(Repository repo, Remote remote)
        {
            if (remote.FetchRefSpecs.Any(r => r.Source == "refs/heads/*"))
                return;

            var allBranchesFetchRefSpec = string.Format("+refs/heads/*:refs/remotes/{0}/*", remote.Name);

            Logger.WriteInfo(string.Format("Adding refspec: {0}", allBranchesFetchRefSpec));

            repo.Network.Remotes.Update(remote,
                r => r.FetchRefSpecs.Add(allBranchesFetchRefSpec));
        }

        static FetchOptions BuildFetchOptions(string username, string password)
        {
            var fetchOptions = new FetchOptions();

            if (!string.IsNullOrEmpty(username))
            {
                fetchOptions.CredentialsProvider = (url, user, types) => new UsernamePasswordCredentials
                {
                    Username = username,
                    Password = password
                };
            }

            return fetchOptions;
        }

        static void CreateFakeBranchPointingAtThePullRequestTip(Repository repo, Authentication authentication)
        {
            var remote = repo.Network.Remotes.Single();

            var remoteTips = string.IsNullOrEmpty(authentication.Username) ?
                GetRemoteTipsForAnonymousUser(repo, remote) :
                GetRemoteTipsUsingUsernamePasswordCredentials(repo, remote, authentication.Username, authentication.Password);

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

            var canonicalName = refs[0].CanonicalName;
            Logger.WriteInfo(string.Format("Found remote tip '{0}' pointing at the commit '{1}'.", canonicalName, headTipSha));

            if (!canonicalName.StartsWith("refs/pull/") && !canonicalName.StartsWith("refs/pull-requests/"))
            {
                var message = string.Format("Remote tip '{0}' from remote '{1}' doesn't look like a valid pull request.", canonicalName, remote.Url);
                throw new WarningException(message);
            }

            var fakeBranchName = canonicalName.Replace("refs/pull/", "refs/heads/pull/").Replace("refs/pull-requests/", "refs/heads/pull-requests/");

            Logger.WriteInfo(string.Format("Creating fake local branch '{0}'.", fakeBranchName));
            repo.Refs.Add(fakeBranchName, new ObjectId(headTipSha));

            Logger.WriteInfo(string.Format("Checking local branch '{0}' out.", fakeBranchName));
            repo.Checkout(fakeBranchName);
        }

        static IEnumerable<DirectReference> GetRemoteTipsUsingUsernamePasswordCredentials(Repository repo, Remote remote, string username, string password)
        {
            return repo.Network.ListReferences(remote, (url, fromUrl, types) => new UsernamePasswordCredentials
                                                                                {
                                                                                    Username = username,
                                                                                    Password = password
                                                                                });
        }

        static IEnumerable<DirectReference> GetRemoteTipsForAnonymousUser(Repository repo, Remote remote)
        {
            return repo.Network.ListReferences(remote);
        }

        static void CreateMissingLocalBranchesFromRemoteTrackingOnes(Repository repo, string remoteName)
        {
            var prefix = string.Format("refs/remotes/{0}/", remoteName);
            var remoteHeadCanonicalName = string.Format("{0}{1}", prefix, "HEAD");

            foreach (var remoteTrackingReference in repo.Refs.FromGlob(prefix + "*").Where(r => r.CanonicalName != remoteHeadCanonicalName))
            {
                var localCanonicalName = "refs/heads/" + remoteTrackingReference.CanonicalName.Substring(prefix.Length);

                if (repo.Refs.Any(x => x.CanonicalName == localCanonicalName))
                {
                    Logger.WriteInfo(string.Format("Skipping local branch creation since it already exists '{0}'.", remoteTrackingReference.CanonicalName));
                    continue;
                }
                Logger.WriteInfo(string.Format("Creating local branch from remote tracking '{0}'.", remoteTrackingReference.CanonicalName));

                var symbolicReference = remoteTrackingReference as SymbolicReference;
                if (symbolicReference == null)
                {
                    repo.Refs.Add(localCanonicalName, new ObjectId(remoteTrackingReference.TargetIdentifier), true);
                }
                else
                {
                    repo.Refs.Add(localCanonicalName, new ObjectId(symbolicReference.ResolveToDirectReference().TargetIdentifier), true);
                }
            }
        }

        static Remote EnsureOnlyOneRemoteIsDefined(IRepository repo)
        {
            var remotes = repo.Network.Remotes;
            var howMany = remotes.Count();

            if (howMany == 1)
            {
                var remote = remotes.Single();
                Logger.WriteInfo(string.Format("One remote found ({0} -> '{1}').", remote.Name, remote.Url));
                return remote;
            }

            var message = string.Format("{0} remote(s) have been detected. When being run on a TeamCity agent, the Git repository is expected to bear one (and no more than one) remote.", howMany);
            throw new WarningException(message);
        }
    }
}