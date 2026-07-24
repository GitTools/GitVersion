using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class ManagedBranchCollection(ManagedGitRepository repository) : IBranchCollection
{
    private readonly ManagedGitRepository repository = repository.NotNull();

    public IEnumerator<IBranch> GetEnumerator() => this.repository.Session.Branches.Cast<IBranch>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IBranch? this[string name]
    {
        get
        {
            name = name.NotNull();
            return this.repository.Session.FindBranch(name);
        }
    }

    public IEnumerable<IBranch> ExcludeBranches(IEnumerable<IBranch> branchesToExclude)
    {
        var toExclude = branchesToExclude as IBranch[] ?? [.. branchesToExclude];

        return this.Where(BranchIsNotExcluded);

        bool BranchIsNotExcluded(IBranch branch) => toExclude.All(branchToExclude => !branch.Equals(branchToExclude));
    }

    public void UpdateTrackedBranch(IBranch branch, string remoteTrackingReferenceName)
    {
        branch.NotNull();
        remoteTrackingReferenceName.NotNull();

        if (!remoteTrackingReferenceName.StartsWith(ReferenceName.RemoteTrackingBranchPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The reference '{remoteTrackingReferenceName}' is not a remote-tracking branch.");
        }

        var remoteAndBranch = remoteTrackingReferenceName[ReferenceName.RemoteTrackingBranchPrefix.Length..];
        var separator = remoteAndBranch.IndexOf('/');

        if (separator <= 0 || separator == remoteAndBranch.Length - 1)
        {
            throw new InvalidOperationException($"The reference '{remoteTrackingReferenceName}' is not a remote-tracking branch.");
        }

        var remoteName = remoteAndBranch[..separator];
        var mergeReference = ReferenceName.LocalBranchPrefix + remoteAndBranch[(separator + 1)..];
        var friendlyName = branch.Name.Friendly;

        var workingDirectory = this.repository.CliWorkingDirectory;
        this.repository.CliMutator.SetConfig(workingDirectory, $"branch.{friendlyName}.remote", remoteName);
        this.repository.CliMutator.SetConfig(workingDirectory, $"branch.{friendlyName}.merge", mergeReference);
        this.repository.Invalidate();
    }
}
