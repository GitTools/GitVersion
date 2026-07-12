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

    public void CreateBranchForPullRequestBranch(AuthenticationInfo auth) =>
        PullRequestBranchOperations.CreateBranchForPullRequestBranch(
            this,
            this.logger,
            remoteName => CliMutator.ListRemoteReferences(CliWorkingDirectory, remoteName, auth));
}
