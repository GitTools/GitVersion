namespace GitVersion;

public interface ICommit : IEquatable<ICommit?>, IComparable<ICommit>, IGitObject
{
    IReadOnlyList<ICommit> Parents { get; }

    DateTimeOffset When { get; }

    string Message { get; }
}
