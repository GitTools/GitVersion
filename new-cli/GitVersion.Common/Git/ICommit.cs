namespace GitVersion.Git;

public interface ICommit : IEquatable<ICommit?>, IComparable<ICommit>, IGitObject
{
    IEnumerable<ICommit> Parents { get; }
    DateTimeOffset When { get; }
    string Message { get; }
}
