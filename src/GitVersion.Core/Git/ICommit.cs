namespace GitVersion;

public interface ICommit : IEquatable<ICommit?>, IComparable<ICommit>, IGitObject
{
    public bool IsMergeCommit => Parents.Count() >= 2;

    IEnumerable<ICommit> Parents { get; }
    DateTimeOffset When { get; }
    string Message { get; }
}
