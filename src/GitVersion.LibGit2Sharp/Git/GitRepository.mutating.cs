using GitVersion.Extensions;
using GitVersion.Helpers;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace GitVersion.Git;

internal partial class GitRepository(ILogger<GitRepository> logger, IGitCliMutator? cliMutator = null) : IMutatingGitRepository
{
    private readonly ILogger<GitRepository> logger = logger.NotNull();
    private readonly IGitCliMutator? cliMutator = cliMutator;

    /// <summary>
    /// Selects the Git backend implementation. 'managed' opts into the new implementation stack
    /// as far as it exists — currently mutating and network operations through the git CLI, with
    /// the managed reader to follow. The default is 'libgit2' in v7.0 and flips to 'managed' in
    /// v7.1 (with 'libgit2' as the fallback); both backends ship side by side until libgit2 is
    /// removed. See https://github.com/GitTools/GitVersion/issues/5031.
    /// </summary>
    private bool UseCliBackend =>
        this.cliMutator is not null
        && string.Equals(SysEnv.GetEnvironmentVariable("GITVERSION_GIT_BACKEND"), "managed", StringComparison.OrdinalIgnoreCase);

    private string CliWorkingDirectory => RepositoryInstance.Info.WorkingDirectory ?? RepositoryInstance.Info.Path;

    public void Clone(string? sourceUrl, string? workdirPath, AuthenticationInfo auth)
    {
        if (UseCliBackend)
        {
            this.cliMutator!.Clone(sourceUrl, workdirPath, auth);
            return;
        }

        try
        {
            var path = Repository.Clone(sourceUrl, workdirPath, GetCloneOptions(auth));
            this.logger.LogInformation("Returned path after repository clone: {Path}", path);
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
                throw new InvalidOperationException("Unauthorized: Incorrect username/password", ex);
            }

            if (message.Contains("403"))
            {
                throw new InvalidOperationException("Forbidden: Possibly Incorrect username/password", ex);
            }

            if (message.Contains("404"))
            {
                throw new InvalidOperationException("Not found: The repository was not found", ex);
            }

            throw new InvalidOperationException("There was an unknown problem with the Git repository you provided", ex);
        }
    }
    public void Checkout(string commitOrBranchSpec)
    {
        if (UseCliBackend)
        {
            RepositoryExtensions.RunSafe(() => this.cliMutator!.Checkout(CliWorkingDirectory, commitOrBranchSpec));
            ResetCachedCollections();
            return;
        }

        RepositoryExtensions.RunSafe(() =>
            Commands.Checkout(RepositoryInstance, commitOrBranchSpec));
    }
    public void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string? logMessage)
    {
        if (UseCliBackend)
        {
            RepositoryExtensions.RunSafe(() => this.cliMutator!.Fetch(CliWorkingDirectory, remote, refSpecs, auth));
            ResetCachedCollections();
            return;
        }

        RepositoryExtensions.RunSafe(() =>
            Commands.Fetch((Repository)RepositoryInstance, remote, refSpecs, GetFetchOptions(auth), logMessage));
    }
    public void CreateBranchForPullRequestBranch(AuthenticationInfo auth) => RepositoryExtensions.RunSafe(() =>
    {
        this.logger.LogInformation("Fetching remote refs to see if there is a pull request ref");

        // FIX ME: What to do when Tip is null?
        if (Head.Tip == null)
        {
            return;
        }

        var headTipSha = Head.Tip.Sha;
        var remote = RepositoryInstance.Network.Remotes.Single();
        var (canonicalName, targetSha) = GetPullRequestReference(auth, remote, headTipSha);
        var referenceName = ReferenceName.Parse(canonicalName);
        this.logger.LogInformation("Found remote tip '{CanonicalName}' pointing at the commit '{HeadTipSha}'.", canonicalName, headTipSha);

        if (referenceName.IsTag)
        {
            this.logger.LogInformation("Checking out tag '{CanonicalName}'", canonicalName);
            Checkout(targetSha);
        }
        else if (referenceName.IsPullRequest)
        {
            var fakeBranchName = canonicalName.Replace("refs/pull/", "refs/heads/pull/").Replace("refs/pull-requests/", "refs/heads/pull-requests/");

            this.logger.LogInformation("Creating fake local branch '{FakeBranchName}'.", fakeBranchName);
            References.Add(fakeBranchName, headTipSha);

            this.logger.LogInformation("Checking local branch '{FakeBranchName}' out.", fakeBranchName);
            Checkout(fakeBranchName);
        }
        else
        {
            var message = $"Remote tip '{canonicalName}' from remote '{remote.Url}' doesn't look like a valid pull request.";
            throw new WarningException(message);
        }
    });
    private (string CanonicalName, string TargetSha) GetPullRequestReference(AuthenticationInfo auth, LibGit2Sharp.Remote remote, string headTipSha)
    {
        var remoteTips = ListRemoteReferences(auth, remote);

        var remoteRefsList = string.Join(FileSystemHelper.Path.NewLine, remoteTips.Select(r => r.CanonicalName));
        this.logger.LogInformation("""
                                   Remote Refs:
                                   {RemoteRefsList}
                                   """, remoteRefsList);
        var refs = remoteTips.Where(r => r.TargetSha == headTipSha).ToList();

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

        return (refs[0].CanonicalName, refs[0].TargetSha);
    }
    private List<GitRemoteReference> ListRemoteReferences(AuthenticationInfo auth, LibGit2Sharp.Remote remote)
    {
        if (UseCliBackend)
        {
            return [.. this.cliMutator!.ListRemoteReferences(CliWorkingDirectory, remote.Name, auth)];
        }

        var network = RepositoryInstance.Network;
        var credentialsProvider = GetCredentialsProvider(auth);
        var references = credentialsProvider != null
            ? network.ListReferences(remote, credentialsProvider)
            : network.ListReferences(remote);
        return [.. references.Select(r => r.ResolveToDirectReference()).Select(r => new GitRemoteReference(r.CanonicalName, r.TargetIdentifier))];
    }
    private static FetchOptions GetFetchOptions(AuthenticationInfo auth) =>
        new() { CredentialsProvider = GetCredentialsProvider(auth) };
    private static CloneOptions GetCloneOptions(AuthenticationInfo auth) =>
        new() { Checkout = false, FetchOptions = { CredentialsProvider = GetCredentialsProvider(auth) } };
    private static CredentialsHandler? GetCredentialsProvider(AuthenticationInfo auth)
    {
        if (!auth.Username.IsNullOrWhiteSpace())
        {
            return (_, _, _) => new UsernamePasswordCredentials { Username = auth.Username, Password = auth.Password ?? string.Empty };
        }

        return null;
    }
}
