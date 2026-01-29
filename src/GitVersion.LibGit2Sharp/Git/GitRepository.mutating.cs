using GitVersion.Extensions;
using GitVersion.Helpers;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace GitVersion.Git;

internal partial class GitRepository(ILogger<GitRepository> logger) : IMutatingGitRepository
{
    private readonly ILogger<GitRepository> logger = logger.NotNull();

    public void Clone(string? sourceUrl, string? workdirPath, AuthenticationInfo auth)
    {
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
    public void CreateBranchForPullRequestBranch(AuthenticationInfo auth) => RepositoryExtensions.RunSafe(() =>
    {
        this.logger.LogInformation("Fetching remote refs to see if there is a pull request ref");

        // FIX ME: What to do when Tip is null?
        if (Head.Tip == null)
            return;

        var headTipSha = Head.Tip.Sha;
        var remote = RepositoryInstance.Network.Remotes.Single();
        var reference = GetPullRequestReference(auth, remote, headTipSha);
        var canonicalName = reference.CanonicalName;
        var referenceName = ReferenceName.Parse(reference.CanonicalName);
        this.logger.LogInformation("Found remote tip '{CanonicalName}' pointing at the commit '{HeadTipSha}'.", canonicalName, headTipSha);

        if (referenceName.IsTag)
        {
            this.logger.LogInformation("Checking out tag '{CanonicalName}'", canonicalName);
            Checkout(reference.Target.Sha);
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
    private DirectReference GetPullRequestReference(AuthenticationInfo auth, LibGit2Sharp.Remote remote, string headTipSha)
    {
        var network = RepositoryInstance.Network;
        var credentialsProvider = GetCredentialsProvider(auth);
        var remoteTips = (credentialsProvider != null
                ? network.ListReferences(remote, credentialsProvider)
                : network.ListReferences(remote))
            .Select(r => r.ResolveToDirectReference()).ToList();

        this.logger.LogInformation($"Remote Refs:{FileSystemHelper.Path.NewLine}" + string.Join(FileSystemHelper.Path.NewLine, remoteTips.Select(r => r.CanonicalName)));
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

        return refs[0];
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
