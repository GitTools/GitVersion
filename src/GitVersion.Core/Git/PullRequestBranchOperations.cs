namespace GitVersion.Git;

/// <summary>
/// The backend-agnostic part of turning a pull-request ref into a real local branch:
/// finding the remote tip that points at HEAD and checking out a fake local branch for it.
/// Both Git backends drive this with their own way of listing remote references.
/// </summary>
internal static class PullRequestBranchOperations
{
    /// <summary>
    /// Creates a local branch that tracks the pull-request ref pointing at the current HEAD tip.
    /// </summary>
    /// <param name="repository">The repository to operate on.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="listRemoteReferences">Lists the references advertised by the remote with the given name.</param>
    public static void CreateBranchForPullRequestBranch(
        IMutatingGitRepository repository,
        ILogger logger,
        Func<string, IReadOnlyList<GitRemoteReference>> listRemoteReferences)
    {
        logger.LogInformation("Fetching remote refs to see if there is a pull request ref");

        // FIX ME: What to do when Tip is null?
        if (repository.Head.Tip == null)
        {
            return;
        }

        var headTipSha = repository.Head.Tip.Sha;
        var remote = repository.Remotes.Single();
        var (canonicalName, targetSha) = GetPullRequestReference(logger, remote, listRemoteReferences(remote.Name), headTipSha);
        var referenceName = ReferenceName.Parse(canonicalName);
        logger.LogInformation("Found remote tip '{CanonicalName}' pointing at the commit '{HeadTipSha}'.", canonicalName, headTipSha);

        if (referenceName.IsTag)
        {
            logger.LogInformation("Checking out tag '{CanonicalName}'", canonicalName);
            repository.Checkout(targetSha);
        }
        else if (referenceName.IsPullRequest)
        {
            var fakeBranchName = canonicalName.Replace("refs/pull/", "refs/heads/pull/").Replace("refs/pull-requests/", "refs/heads/pull-requests/");

            logger.LogInformation("Creating fake local branch '{FakeBranchName}'.", fakeBranchName);
            repository.References.Add(fakeBranchName, headTipSha);

            logger.LogInformation("Checking local branch '{FakeBranchName}' out.", fakeBranchName);
            repository.Checkout(fakeBranchName);
        }
        else
        {
            var message = $"Remote tip '{canonicalName}' from remote '{remote.Url}' doesn't look like a valid pull request.";
            throw new WarningException(message);
        }
    }

    private static (string CanonicalName, string TargetSha) GetPullRequestReference(
        ILogger logger,
        IRemote remote,
        IReadOnlyList<GitRemoteReference> remoteTips,
        string headTipSha)
    {
        var remoteRefsList = string.Join(SysEnv.NewLine, remoteTips.Select(r => r.CanonicalName));
        logger.LogInformation("""
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
