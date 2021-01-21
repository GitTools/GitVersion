using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion.Logging;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace GitVersion
{
    internal sealed class GitRepository : IGitRepository
    {
        private readonly ILog log;
        private Lazy<IRepository> repositoryLazy;
        private IRepository repositoryInstance => repositoryLazy.Value;

        public GitRepository(ILog log, IGitRepositoryInfo repositoryInfo)
            : this(log, () => repositoryInfo.GitRootPath)
        {
        }

        internal GitRepository(string gitRootDirectory)
            : this(new NullLog(), () => gitRootDirectory)
        {
        }

        internal GitRepository(IRepository repository)
        {
            log = new NullLog();
            repositoryLazy = new Lazy<IRepository>(() => repository);
        }

        private GitRepository(ILog log, Func<string> getGitRootDirectory)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            repositoryLazy = new Lazy<IRepository>(() => new Repository(getGitRootDirectory()));
        }

        public void Clone(string sourceUrl, string workdirPath, AuthenticationInfo auth)
        {
            try
            {
                var path = Repository.Clone(sourceUrl, workdirPath, GetCloneOptions(auth));
                log.Info($"Returned path after repository clone: {path}");
            }
            catch (LibGit2SharpException ex)
            {
                var message = ex.Message;
                if (message.Contains("401"))
                {
                    throw new Exception("Unauthorized: Incorrect username/password");
                }
                if (message.Contains("403"))
                {
                    throw new Exception("Forbidden: Possibly Incorrect username/password");
                }
                if (message.Contains("404"))
                {
                    throw new Exception("Not found: The repository was not found");
                }

                throw new Exception("There was an unknown problem with the Git repository you provided", ex);
            }
        }
        public void Dispose()
        {
            if (repositoryLazy.IsValueCreated) repositoryInstance.Dispose();
        }

        public string Path => repositoryInstance.Info.Path;
        public string WorkingDirectory => repositoryInstance.Info.WorkingDirectory;
        public bool IsHeadDetached => repositoryInstance.Info.IsHeadDetached;

        public IBranch Head => new Branch(repositoryInstance.Head);

        public ITagCollection Tags => new TagCollection(repositoryInstance.Tags);
        public IReferenceCollection Refs => new ReferenceCollection(repositoryInstance.Refs);
        public IBranchCollection Branches => new BranchCollection(repositoryInstance.Branches);
        public ICommitCollection Commits => new CommitCollection(repositoryInstance.Commits);
        public IRemoteCollection Remotes => new RemoteCollection(repositoryInstance.Network.Remotes);

        public int GetNumberOfUncommittedChanges()
        {
            // check if we have a branch tip at all to behave properly with empty repos
            // => return that we have actually uncomitted changes because we are apparently
            // running GitVersion on something which lives inside this brand new repo _/\Ö/\_
            if (repositoryInstance.Head?.Tip == null || repositoryInstance.Diff == null)
            {
                // this is a somewhat cumbersome way of figuring out the number of changes in the repo
                // which is more expensive than to use the Diff as it gathers more info, but
                // we can't use the other method when we are dealing with a new/empty repo
                try
                {
                    var status = repositoryInstance.RetrieveStatus();
                    return status.Untracked.Count() + status.Staged.Count();
                }
                catch (Exception)
                {
                    return int.MaxValue; // this should be somewhat puzzling to see,
                    // so we may have reached our goal to show that
                    // that repo is really "Dirty"...
                }
            }

            // gets all changes of the last commit vs Staging area and WT
            var changes = repositoryInstance.Diff.Compare<TreeChanges>(repositoryInstance.Head.Tip.Tree,
                DiffTargets.Index | DiffTargets.WorkingDirectory);

            return changes.Count;
        }
        public ICommit FindMergeBase(ICommit commit, ICommit otherCommit)
        {
            var mergeBase = repositoryInstance.ObjectDatabase.FindMergeBase((Commit)commit, (Commit)otherCommit);
            return new Commit(mergeBase);
        }

        public void CreateBranchForPullRequestBranch(AuthenticationInfo auth)
        {
            var network = repositoryInstance.Network;
            var remote = network.Remotes.Single();

            log.Info("Fetching remote refs to see if there is a pull request ref");
            var credentialsProvider = GetCredentialsProvider(auth);
            var remoteTips = (credentialsProvider != null
                    ? network.ListReferences(remote, credentialsProvider)
                    : network.ListReferences(remote))
                .Select(r => r.ResolveToDirectReference()).ToList();

            log.Info($"Remote Refs:{System.Environment.NewLine}" + string.Join(System.Environment.NewLine, remoteTips.Select(r => r.CanonicalName)));

            var headTipSha = Head.Tip?.Sha;

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

            var reference = refs.First();
            var canonicalName = reference.CanonicalName;
            log.Info($"Found remote tip '{canonicalName}' pointing at the commit '{headTipSha}'.");

            if (canonicalName.StartsWith("refs/tags"))
            {
                log.Info($"Checking out tag '{canonicalName}'");
                Checkout(reference.Target.Sha);
                return;
            }

            if (!canonicalName.StartsWith("refs/pull/") && !canonicalName.StartsWith("refs/pull-requests/"))
            {
                var message = $"Remote tip '{canonicalName}' from remote '{remote.Url}' doesn't look like a valid pull request.";
                throw new WarningException(message);
            }

            var fakeBranchName = canonicalName.Replace("refs/pull/", "refs/heads/pull/").Replace("refs/pull-requests/", "refs/heads/pull-requests/");

            log.Info($"Creating fake local branch '{fakeBranchName}'.");
            Refs.Add(fakeBranchName, headTipSha);

            log.Info($"Checking local branch '{fakeBranchName}' out.");
            Checkout(fakeBranchName);
        }
        public IRemote EnsureOnlyOneRemoteIsDefined()
        {
            var remotes = repositoryInstance.Network.Remotes;
            var howMany = remotes.Count();

            if (howMany == 1)
            {
                var remote = remotes.Single();
                log.Info($"One remote found ({remote.Name} -> '{remote.Url}').");
                if (remote.FetchRefSpecs.All(r => r.Source != "refs/heads/*"))
                {
                    var allBranchesFetchRefSpec = $"+refs/heads/*:refs/remotes/{remote.Name}/*";
                    log.Info($"Adding refspec: {allBranchesFetchRefSpec}");
                    repositoryInstance.Network.Remotes.Update(remote.Name, r => r.FetchRefSpecs.Add(allBranchesFetchRefSpec));
                }
                return new Remote(remote);
            }

            var message = $"{howMany} remote(s) have been detected. When being run on a build server, the Git repository is expected to bear one (and no more than one) remote.";
            throw new WarningException(message);
        }
        public void Checkout(string commitOrBranchSpec) => Commands.Checkout(repositoryInstance, commitOrBranchSpec);
        public void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string logMessage) =>
            Commands.Fetch((Repository)repositoryInstance, remote, refSpecs, GetFetchOptions(auth), logMessage);
        internal static string Discover(string path) => Repository.Discover(path);

        private static FetchOptions GetFetchOptions(AuthenticationInfo auth)
        {
            return new()
            {
                CredentialsProvider = GetCredentialsProvider(auth)
            };
        }
        private static CloneOptions GetCloneOptions(AuthenticationInfo auth)
        {
            return new()
            {
                Checkout = false,
                CredentialsProvider = GetCredentialsProvider(auth)
            };
        }
        private static CredentialsHandler? GetCredentialsProvider(AuthenticationInfo auth)
        {
            if (!string.IsNullOrWhiteSpace(auth.Username))
            {
                return (_, _, _) => new UsernamePasswordCredentials
                {
                    Username = auth.Username,
                    Password = auth.Password ?? string.Empty
                };
            }
            return null;
        }
    }
}
