using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed partial class ManagedGitRepository(ILogger<ManagedGitRepository> logger, IGitCliMutator cliMutator) : IMutatingGitRepository
{
    private readonly ILogger<ManagedGitRepository> logger = logger.NotNull();

    internal IGitCliMutator CliMutator { get; } = cliMutator.NotNull();

    public void Clone(string? sourceUrl, string? workdirPath, AuthenticationInfo auth) =>
        CliMutator.Clone(sourceUrl, workdirPath, auth);

    public void Checkout(string commitOrBranchSpec)
    {
        CliMutator.Checkout(CliWorkingDirectory, commitOrBranchSpec);
        Invalidate();
    }

    public void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string? logMessage)
    {
        CliMutator.Fetch(CliWorkingDirectory, remote, refSpecs, auth);
        Invalidate();
    }

    public void CreateBranchForPullRequestBranch(AuthenticationInfo auth)
    {
        this.logger.LogInformation("Fetching remote refs to see if there is a pull request ref");

        // FIX ME: What to do when Tip is null?
        if (Head.Tip == null)
        {
            return;
        }

        var headTipSha = Head.Tip.Sha;
        var remote = Remotes.Single();
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
    }

    private (string CanonicalName, string TargetSha) GetPullRequestReference(AuthenticationInfo auth, IRemote remote, string headTipSha)
    {
        var remoteTips = CliMutator.ListRemoteReferences(CliWorkingDirectory, remote.Name, auth);

        var remoteRefsList = string.Join(SysEnv.NewLine, remoteTips.Select(r => r.CanonicalName));
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
}
