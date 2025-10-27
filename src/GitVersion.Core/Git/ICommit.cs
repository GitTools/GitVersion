namespace GitVersion.Git;

public interface ICommit : IEquatable<ICommit?>, IComparable<ICommit>
{
    IReadOnlyList<ICommit> Parents { get; }

    IObjectId Id { get; }

    string Sha { get; }

    DateTimeOffset When { get; }

    string Message { get; }

    IReadOnlyList<string> DiffPaths { get; }
}
