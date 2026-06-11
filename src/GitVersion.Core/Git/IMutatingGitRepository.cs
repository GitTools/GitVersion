namespace GitVersion.Git;

/// <summary>Extends <see cref="IGitRepository"/> with operations that modify the repository state.</summary>
public interface IMutatingGitRepository : IGitRepository
{
    /// <summary>Creates a local branch that tracks the pull-request ref identified by the current build environment.</summary>
    void CreateBranchForPullRequestBranch(AuthenticationInfo auth);

    /// <summary>Checks out the specified commit or branch.</summary>
    void Checkout(string commitOrBranchSpec);

    /// <summary>Fetches the given <paramref name="refSpecs"/> from <paramref name="remote"/> using the supplied credentials.</summary>
    void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string? logMessage);

    /// <summary>Clones the repository at <paramref name="sourceUrl"/> into <paramref name="workdirPath"/> using the supplied credentials.</summary>
    void Clone(string? sourceUrl, string? workdirPath, AuthenticationInfo auth);
}
