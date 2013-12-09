namespace GitFlowVersion.GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    public static class GitHelper
    {
        public static void NormalizeGitDirectory(string gitDirectory)
        {
            using (var repo = new Repository(gitDirectory))
            {
                EnsureOnlyOneRemoteIsDefined(repo);
                CreateMissingLocalBranchesFromRemoteTrackingOnes(repo);

                if (!repo.Info.IsHeadDetached)
                {
                    return;
                }

                CreateFakeBranchPointingAtThePullRequestTip(repo);
            }
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
            if (!canonicalName.StartsWith("refs/pull/"))
            {
                var message = string.Format("Remote tip '{0}' from remote '{1}' doesn't look like a valid pull request.", canonicalName, remote.Url);
                throw new ErrorException(message);
            }

            var fakeBranchName = canonicalName.Replace("refs/pull/", "refs/heads/pull/");
            repo.Refs.Add(fakeBranchName, new ObjectId(headTipSha));

            repo.Checkout(fakeBranchName);
        }

        static void CreateMissingLocalBranchesFromRemoteTrackingOnes(Repository repo)
        {
            var remoteName = repo.Network.Remotes.Single().Name;
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

        static void EnsureOnlyOneRemoteIsDefined(IRepository repo)
        {
            var howMany = repo.Network.Remotes.Count();

            if (howMany == 1)
            {
                return;
            }

            var message = string.Format("{0} remote(s) have been detected. When being run on a TeamCity agent, the Git repository is expected to bear one (and no more than one) remote.", howMany);
            throw new ErrorException(message);
        }
    }
}