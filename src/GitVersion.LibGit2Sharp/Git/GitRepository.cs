using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace GitVersion;

internal sealed class GitRepository : IMutatingGitRepository
{
    private readonly ILog log;
    private readonly Lazy<IRepository> repositoryLazy;

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
        repository = repository.NotNull();
        this.log = new NullLog();
        this.repositoryLazy = new Lazy<IRepository>(() => repository);
    }

    private GitRepository(ILog log, Func<string?> getGitRootDirectory)
    {
        this.log = log.NotNull();
        this.repositoryLazy = new Lazy<IRepository>(() => new Repository(getGitRootDirectory()));
    }

    private IRepository RepositoryInstance => this.repositoryLazy.Value;
    public string Path => RepositoryInstance.Info.Path;
    public string WorkingDirectory => RepositoryInstance.Info.WorkingDirectory;
    public bool IsHeadDetached => RepositoryInstance.Info.IsHeadDetached;
    public IBranch Head => new Branch(RepositoryInstance.Head);
    public ITagCollection Tags => new TagCollection(RepositoryInstance.Tags);
    public IReferenceCollection Refs => new ReferenceCollection(RepositoryInstance.Refs);
    public IBranchCollection Branches => new BranchCollection(RepositoryInstance.Branches);
    public ICommitCollection Commits => new CommitCollection(RepositoryInstance.Commits);
    public IRemoteCollection Remotes => new RemoteCollection(RepositoryInstance.Network.Remotes);

    public ICommit? FindMergeBase(ICommit commit, ICommit otherCommit)
    {
        commit = commit.NotNull();
        otherCommit = otherCommit.NotNull();

        var retryAction = new RetryAction<LockedFileException, ICommit?>();
        return retryAction.Execute(() =>
        {
            var first = (Commit)commit;
            var second = (Commit)otherCommit;
            var mergeBase = RepositoryInstance.ObjectDatabase.FindMergeBase(first, second);
            return mergeBase == null ? null : new Commit(mergeBase);
        });
    }

    public int GetNumberOfUncommittedChanges()
    {
        var retryAction = new RetryAction<LibGit2Sharp.LockedFileException, int>();
        return retryAction.Execute(GetNumberOfUncommittedChangesInternal);
    }

    public void CreateBranchForPullRequestBranch(AuthenticationInfo auth) => RepositoryExtensions.RunSafe(() =>
    {
        this.log.Info("Fetching remote refs to see if there is a pull request ref");

        // FIX ME: What to do when Tip is null?
        if (Head.Tip == null)
            return;

        var headTipSha = Head.Tip.Sha;
        var remote = RepositoryInstance.Network.Remotes.Single();
        var reference = GetPullRequestReference(auth, remote, headTipSha);
        var canonicalName = reference.CanonicalName;
        var referenceName = ReferenceName.Parse(reference.CanonicalName);
        this.log.Info($"Found remote tip '{canonicalName}' pointing at the commit '{headTipSha}'.");

        if (referenceName.IsTag)
        {
            this.log.Info($"Checking out tag '{canonicalName}'");
            Checkout(reference.Target.Sha);
        }
        else if (referenceName.IsPullRequest)
        {
            var fakeBranchName = canonicalName.Replace("refs/pull/", "refs/heads/pull/").Replace("refs/pull-requests/", "refs/heads/pull-requests/");

            this.log.Info($"Creating fake local branch '{fakeBranchName}'.");
            Refs.Add(fakeBranchName, headTipSha);

            this.log.Info($"Checking local branch '{fakeBranchName}' out.");
            Checkout(fakeBranchName);
        }
        else
        {
            var message = $"Remote tip '{canonicalName}' from remote '{remote.Url}' doesn't look like a valid pull request.";
            throw new WarningException(message);
        }
    });

    public void Clone(string? sourceUrl, string? workdirPath, AuthenticationInfo auth)
    {
        try
        {
            var path = Repository.Clone(sourceUrl, workdirPath, GetCloneOptions(auth));
            this.log.Info($"Returned path after repository clone: {path}");
        }
        catch (LibGit2Sharp.LockedFileException ex)
        {
            throw new LockedFileException(ex);
        }
        catch (LibGit2SharpException ex)
        {
            var message = ex.Message;
            if (message.Contains("401"))
            {
                throw new Exception("Unauthorized: Incorrect username/password", ex);
            }

            if (message.Contains("403"))
            {
                throw new Exception("Forbidden: Possibly Incorrect username/password", ex);
            }

            if (message.Contains("404"))
            {
                throw new Exception("Not found: The repository was not found", ex);
            }

            throw new Exception("There was an unknown problem with the Git repository you provided", ex);
        }
    }

    public void Checkout(string commitOrBranchSpec) =>
        RepositoryExtensions.RunSafe(() =>
            Commands.Checkout(RepositoryInstance, commitOrBranchSpec));

    public void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string? logMessage) =>
        RepositoryExtensions.RunSafe(() =>
            Commands.Fetch((Repository)RepositoryInstance, remote, refSpecs, GetFetchOptions(auth), logMessage));

    private DirectReference GetPullRequestReference(AuthenticationInfo auth, LibGit2Sharp.Remote remote, string headTipSha)
    {
        var network = RepositoryInstance.Network;
        var credentialsProvider = GetCredentialsProvider(auth);
        var remoteTips = (credentialsProvider != null
                ? network.ListReferences(remote, credentialsProvider)
                : network.ListReferences(remote))
            .Select(r => r.ResolveToDirectReference()).ToList();

        this.log.Info($"Remote Refs:{System.Environment.NewLine}" + string.Join(System.Environment.NewLine, remoteTips.Select(r => r.CanonicalName)));
        var refs = remoteTips.Where(r => r.TargetIdentifier == headTipSha).ToList();

        switch (refs.Count)
        {
            case 0:
                {
                    var message = $"Couldn't find any remote tips from remote '{remote.Url}' pointing at the commit '{headTipSha}'.";
                    throw new WarningException(message);
                }
            case > 1:
                {
                    var names = string.Join(", ", refs.Select(r => r.CanonicalName));
                    var message = $"Found more than one remote tip from remote '{remote.Url}' pointing at the commit '{headTipSha}'. Unable to determine which one to use ({names}).";
                    throw new WarningException(message);
                }
        }

        return refs.First();
    }

    public void Dispose()
    {
        if (this.repositoryLazy.IsValueCreated) RepositoryInstance.Dispose();
    }

    private int GetNumberOfUncommittedChangesInternal()
    {
        // check if we have a branch tip at all to behave properly with empty repos
        // => return that we have actually un-committed changes because we are apparently
        // running GitVersion on something which lives inside this brand new repo _/\Ö/\_
        if (RepositoryInstance.Head?.Tip == null || RepositoryInstance.Diff == null)
        {
            // this is a somewhat cumbersome way of figuring out the number of changes in the repo
            // which is more expensive than to use the Diff as it gathers more info, but
            // we can't use the other method when we are dealing with a new/empty repo
            try
            {
                var status = RepositoryInstance.RetrieveStatus();
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
        var changes = RepositoryInstance.Diff.Compare<TreeChanges>(RepositoryInstance.Head.Tip.Tree,
            DiffTargets.Index | DiffTargets.WorkingDirectory);

        return changes.Count;
    }

    internal static string? Discover(string? path) => Repository.Discover(path);

    private static FetchOptions GetFetchOptions(AuthenticationInfo auth) =>
        new() { CredentialsProvider = GetCredentialsProvider(auth) };

    private static CloneOptions GetCloneOptions(AuthenticationInfo auth) =>
        new() { Checkout = false, CredentialsProvider = GetCredentialsProvider(auth) };

    private static CredentialsHandler? GetCredentialsProvider(AuthenticationInfo auth)
    {
        if (!auth.Username.IsNullOrWhiteSpace())
        {
            return (_, _, _) => new UsernamePasswordCredentials { Username = auth.Username, Password = auth.Password ?? string.Empty };
        }

        return null;
    }
}
