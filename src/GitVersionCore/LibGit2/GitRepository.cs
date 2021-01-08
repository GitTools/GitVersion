using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion.Logging;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace GitVersion
{
    public class GitRepository : IGitRepository
    {
        private Lazy<IRepository> repositoryLazy;
        private IRepository repositoryInstance => repositoryLazy.Value;

        public GitRepository(IGitRepositoryInfo repositoryInfo)
            : this(() => repositoryInfo.GitRootPath)
        {
        }
        internal GitRepository(string gitRootDirectory)
            : this(() => gitRootDirectory)
        {
        }

        internal GitRepository(IRepository repository)
        {
            repositoryLazy = new Lazy<IRepository>(() => repository);
        }

        private GitRepository(Func<string> getGitRootDirectory)
        {
            repositoryLazy = new Lazy<IRepository>(() => new Repository(getGitRootDirectory()));
        }

        public static string Discover(string path) => Repository.Discover(path);
        public static string Clone(string sourceUrl, string workdirPath, AuthenticationInfo auth)
        {
            try
            {
                return Repository.Clone(sourceUrl, workdirPath, GetCloneOptions(auth));
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
                    return Int32.MaxValue; // this should be somewhat puzzling to see,
                    // so we may have reached our goal to show that
                    // that repo is really "Dirty"...
                }
            }

            // gets all changes of the last commit vs Staging area and WT
            var changes = repositoryInstance.Diff.Compare<TreeChanges>(repositoryInstance.Head.Tip.Tree,
                DiffTargets.Index | DiffTargets.WorkingDirectory);

            return changes.Count;
        }
        public Commit FindMergeBase(Commit commit, Commit otherCommit)
        {
            return (Commit)repositoryInstance.ObjectDatabase.FindMergeBase(commit, otherCommit);
        }
        public string ShortenObjectId(Commit commit)
        {
            return repositoryInstance.ObjectDatabase.ShortenObjectId(commit);
        }

        public IBranch Head => (Branch)repositoryInstance.Head;

        public ReferenceCollection Refs => (ReferenceCollection)repositoryInstance.Refs;

        public CommitCollection Commits => CommitCollection.FromCommitLog(repositoryInstance.Commits);

        public BranchCollection Branches => (BranchCollection)repositoryInstance.Branches;

        public TagCollection Tags => (TagCollection)repositoryInstance.Tags;

        public void CreateBranchForPullRequestBranch(ILog log, AuthenticationInfo auth)
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

            var headTipSha = Head.Tip.Sha;

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
        public bool GitRepoHasMatchingRemote(string targetUrl)
        {
            return repositoryInstance.Network.Remotes.Any(r => r.Url == targetUrl);
        }
        public void CleanupDuplicateOrigin(string defaultRemoteName)
        {
            var remoteToKeep = defaultRemoteName;
            // check that we have a remote that matches defaultRemoteName if not take the first remote
            if (!repositoryInstance.Network.Remotes.Any(remote => remote.Name.Equals(defaultRemoteName, StringComparison.InvariantCultureIgnoreCase)))
            {
                remoteToKeep = repositoryInstance.Network.Remotes.First().Name;
            }

            var duplicateRepos = repositoryInstance.Network
                .Remotes
                .Where(remote => !remote.Name.Equals(remoteToKeep, StringComparison.InvariantCultureIgnoreCase))
                .Select(remote => remote.Name);

            // remove all remotes that are considered duplicates
            foreach (var repoName in duplicateRepos)
            {
                repositoryInstance.Network.Remotes.Remove(repoName);
            }
        }
        public IRemote EnsureOnlyOneRemoteIsDefined(ILog log)
        {
            var remotes = repositoryInstance.Network.Remotes;
            var howMany = remotes.Count();

            if (howMany == 1)
            {
                var remote = remotes.Single();
                log.Info($"One remote found ({remote.Name} -> '{remote.Url}').");
                AddMissingRefSpecs(log, remote);
                return (Remote)remote;
            }

            var message = $"{howMany} remote(s) have been detected. When being run on a build server, the Git repository is expected to bear one (and no more than one) remote.";
            throw new WarningException(message);
        }

        private void AddMissingRefSpecs(ILog log, LibGit2Sharp.Remote remote)
        {
            if (remote.FetchRefSpecs.Any(r => r.Source == "refs/heads/*"))
                return;

            var allBranchesFetchRefSpec = $"+refs/heads/*:refs/remotes/{remote.Name}/*";
            log.Info($"Adding refspec: {allBranchesFetchRefSpec}");
            repositoryInstance.Network.Remotes.Update(remote.Name, r => r.FetchRefSpecs.Add(allBranchesFetchRefSpec));
        }

        public static FetchOptions GetFetchOptions(AuthenticationInfo auth)
        {
            return new FetchOptions
            {
                CredentialsProvider = GetCredentialsProvider(auth)
            };
        }
        private static CloneOptions GetCloneOptions(AuthenticationInfo auth)
        {
            return new CloneOptions
            {
                Checkout = false,
                CredentialsProvider = GetCredentialsProvider(auth)
            };
        }
        private static CredentialsHandler GetCredentialsProvider(AuthenticationInfo auth)
        {
            if (!string.IsNullOrWhiteSpace(auth.Username))
            {
                return (url, user, types) => new UsernamePasswordCredentials
                {
                    Username = auth.Username,
                    Password = auth.Password ?? string.Empty
                };
            }
            return null;
        }

        public bool GetMatchingCommitBranch(Commit baseVersionSource, IBranch branch, Commit firstMatchingCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = branch,
                ExcludeReachableFrom = baseVersionSource,
                FirstParentOnly = true,
            };
            var commitCollection = Commits.QueryBy(filter);

            return commitCollection.Contains(firstMatchingCommit);
        }
        public IEnumerable<Commit> GetCommitsReacheableFrom(Commit commit, IBranch branch)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = branch
            };
            var commitCollection = Commits.QueryBy(filter);

            return commitCollection.Where(c => c.Sha == commit.Sha);
        }
        public List<Commit> GetCommitsReacheableFromHead(Commit headCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = headCommit,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
            };

            var commitCollection = Commits.QueryBy(filter);

            return commitCollection.ToList();
        }
        public Commit GetForwardMerge(Commit commitToFindCommonBase, Commit findMergeBase)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = commitToFindCommonBase,
                ExcludeReachableFrom = findMergeBase
            };
            var commitCollection = Commits.QueryBy(filter);

            var forwardMerge = commitCollection
                .FirstOrDefault(c => c.Parents.Contains(findMergeBase));
            return forwardMerge;
        }
        public IEnumerable<Commit> GetMergeBaseCommits(Commit mergeCommit, Commit mergedHead, Commit findMergeBase)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = mergedHead,
                ExcludeReachableFrom = findMergeBase
            };
            var commitCollection = Commits.QueryBy(filter);

            var commits = mergeCommit != null
                ? new[]
                {
                    mergeCommit
                }.Union(commitCollection)
                : commitCollection;
            return commits.ToList();
        }
        public Commit GetBaseVersionSource(Commit currentBranchTip)
        {
            try
            {
                var filter = new CommitFilter
                {
                    IncludeReachableFrom = currentBranchTip
                };
                var commitCollection = Commits.QueryBy(filter);

                var baseVersionSource = commitCollection.First(c => !c.Parents.Any());
                return baseVersionSource;
            }
            catch (NotFoundException exception)
            {
                throw new GitVersionException($"Cannot find commit {currentBranchTip.Sha}. Please ensure that the repository is an unshallow clone with `git fetch --unshallow`.", exception);
            }
        }
        public List<Commit> GetMainlineCommitLog(Commit baseVersionSource, Commit mainlineTip)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = mainlineTip,
                ExcludeReachableFrom = baseVersionSource,
                SortBy = CommitSortStrategies.Reverse,
                FirstParentOnly = true
            };
            var commitCollection = Commits.QueryBy(filter);

            var mainlineCommitLog = commitCollection.ToList();
            return mainlineCommitLog;
        }
        public CommitCollection GetCommitLog(Commit baseVersionSource, Commit currentCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = currentCommit,
                ExcludeReachableFrom = baseVersionSource,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            var commitCollection = Commits.QueryBy(filter);

            return commitCollection;
        }
        public void Checkout(string committishOrBranchSpec)
        {
            Commands.Checkout(repositoryInstance, committishOrBranchSpec);
        }

        public void Checkout(IBranch branch)
        {
            Commands.Checkout(repositoryInstance, (Branch)branch);
        }

        public void Fetch(string remote, IEnumerable<string> refspecs, AuthenticationInfo auth, string logMessage)
        {
            Commands.Fetch((Repository)repositoryInstance, remote, refspecs, GitRepository.GetFetchOptions(auth), logMessage);
        }
    }
}
