using GitVersion.Git;

namespace GitVersion;

public interface IGitPreparer
{
    void Prepare();
    void EnsureLocalBranchExistsForCurrentBranch(IRemote remote, string currentBranch);
}
