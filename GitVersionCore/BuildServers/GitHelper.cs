namespace GitVersion
{
    using System.Linq;
    using LibGit2Sharp;

    public static class GitHelper
    {
        public static void NormalizeGitDirectory(string gitDirectory, Arguments arguments, string branch = null)
        {
            using (var repo = new Repository(gitDirectory))
            {
                var remote = EnsureOnlyOneRemoteIsDefined(repo);

                AddMissingRefSpecs(repo, remote);

                Logger.WriteInfo(string.Format("Fetching from remote '{0}' using the following refspecs: {1}.",
                    remote.Name, string.Join(", ", remote.FetchRefSpecs.Select(r => r.Specification))));

                var fetchOptions = BuildFetchOptions(arguments.Username, arguments.Password);
                repo.Network.Fetch(remote, fetchOptions);

                CreateMissingLocalBranchesFromRemoteTrackingOnes(repo, remote.Name);

                if (!repo.Info.IsHeadDetached)
                {
                    Logger.WriteInfo(string.Format("HEAD points at branch '{0}'.", repo.Refs.Head.TargetIdentifier));
                    return;
                }

                Logger.WriteInfo(string.Format("HEAD is detached and points at commit '{0}'.", repo.Refs.Head.TargetIdentifier));

                if (branch != null)
                {
                    Logger.WriteInfo(string.Format("Checking out local branch 'refs/heads/{0}'.", branch));
                    repo.Checkout("refs/heads/" + branch);
                }
                else
                {
                    CreateFakeBranchPointingAtThePullRequestTip(repo);
                }
            }
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
                fetchOptions.Credentials = new UsernamePasswordCredentials
                {
                    Username = username,
                    Password = password
                };
            }

            return fetchOptions;
        }

        static void CreateFakeBranchPointingAtThePullRequestTip(Repository repo)
        {
            var remote = repo.Network.Remotes.Single();
            var remoteTips = repo.Network.ListReferences(remote);

            var headTipSha = repo.Head.Tip.Sha;

            var refs = remoteTips.Where(r => r.TargetIdentifier == headTipSha).ToList();

            if (refs.Count == 0)
            {
                var message = string.Format("Couldn't find any remote tips from remote '{0}' pointing at the commit '{1}'.", remote.Url, headTipSha);
                throw new ErrorException(message);
            }

            if (refs.Count > 1)
            {
                var names = string.Join(", ", refs.Select(r => r.CanonicalName));
                var message = string.Format("Found more than one remote tip from remote '{0}' pointing at the commit '{1}'. Unable to determine which one to use ({2}).", remote.Url, headTipSha, names);
                throw new ErrorException(message);
            }

            var canonicalName = refs[0].CanonicalName;
            Logger.WriteInfo(string.Format("Found remote tip '{0}' pointing at the commit '{1}'.", canonicalName, headTipSha));

            if (!canonicalName.StartsWith("refs/pull/"))
            {
                var message = string.Format("Remote tip '{0}' from remote '{1}' doesn't look like a valid pull request.", canonicalName, remote.Url);
                throw new ErrorException(message);
            }

            var fakeBranchName = canonicalName.Replace("refs/pull/", "refs/heads/pull/");

            Logger.WriteInfo(string.Format("Creating fake local branch '{0}'.", fakeBranchName));
            repo.Refs.Add(fakeBranchName, new ObjectId(headTipSha));

            Logger.WriteInfo(string.Format("Checking local branch '{0}' out.", fakeBranchName));
            repo.Checkout(fakeBranchName);
        }

        static void CreateMissingLocalBranchesFromRemoteTrackingOnes(Repository repo, string remoteName)
        {
            var prefix = string.Format("refs/remotes/{0}/", remoteName);

            foreach (var remoteTrackingReference in repo.Refs.FromGlob(prefix + "*"))
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
            throw new ErrorException(message);
        }
    }
}