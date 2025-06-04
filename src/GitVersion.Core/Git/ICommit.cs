namespace GitVersion.Git;

public interface ICommit : IEquatable<ICommit?>, IComparable<ICommit>, IGitObject
{
    IReadOnlyList<ICommit> Parents { get; }

    DateTimeOffset When { get; }

    string Message { get; }

    IReadOnlyList<string> DiffPaths { get; }
}
