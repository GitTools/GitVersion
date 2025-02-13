namespace GitVersion.Git;

public interface IGitRepository : IDisposable
{
    string Path { get; }
    string WorkingDirectory { get; }
    bool IsHeadDetached { get; }
    bool IsShallow { get; }

    IBranch Head { get; }

    ITagCollection Tags { get; }
    IReferenceCollection Refs { get; }
    IBranchCollection Branches { get; }
    ICommitCollection Commits { get; }
    IRemoteCollection Remotes { get; }

    ICommit? FindMergeBase(ICommit commit, ICommit otherCommit);
    IEnumerable<string>? FindPatchPaths(ICommit commit, string? tagPrefix);
    int UncommittedChangesCount();
    void DiscoverRepository(string? gitDirectory);
}
