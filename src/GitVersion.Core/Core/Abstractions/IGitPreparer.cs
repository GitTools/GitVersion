using GitVersion.Git;

namespace GitVersion;

/// <summary>Prepares the Git repository so that GitVersion can calculate the version (fetches, clones, and normalises refs as needed).</summary>
public interface IGitPreparer
{
    /// <summary>Performs all preparation steps required before version calculation (fetch, clone, normalise).</summary>
    void Prepare();

    /// <summary>Ensures a local branch exists that tracks the supplied <paramref name="currentBranch"/> on the given <paramref name="remote"/>.</summary>
    void EnsureLocalBranchExistsForCurrentBranch(IRemote remote, string currentBranch);
}
