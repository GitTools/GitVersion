namespace GitVersion;

public interface IGitRepository
{
    string Path { get; }
    string WorkingDirectory { get; }
    bool IsHeadDetached { get; }
    IBranch Head { get; }
    ITagCollection Tags { get; }
    IReferenceCollection Refs { get; }
    IBranchCollection Branches { get; }
    ICommitCollection Commits { get; }
    IRemoteCollection Remotes { get; }

    ICommit FindMergeBase(ICommit commit, ICommit otherCommit);
    int GetNumberOfUncommittedChanges();
}
