namespace GitVersion.Git;

/// <summary>Represents the set of all branches in a Git repository.</summary>
public interface IBranchCollection : IEnumerable<IBranch>
{
    /// <summary>Returns the branch with the given <paramref name="name"/>, or <see langword="null"/> if it does not exist.</summary>
    IBranch? this[string name] { get; }

    /// <summary>Returns all branches except those in <paramref name="branchesToExclude"/>.</summary>
    IEnumerable<IBranch> ExcludeBranches(IEnumerable<IBranch> branchesToExclude);

    /// <summary>Updates the remote-tracking branch reference for <paramref name="branch"/> to point to <paramref name="remoteTrackingReferenceName"/>.</summary>
    void UpdateTrackedBranch(IBranch branch, string remoteTrackingReferenceName);
}
