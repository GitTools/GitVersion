namespace GitVersion;

public interface ICommit : IEquatable<ICommit?>, IComparable<ICommit>, IGitObject
{
    bool IsMergeCommit => Parents.Count() >= 2;
    IEnumerable<ICommit> Parents { get; }
    DateTimeOffset When { get; }
    string Message { get; }
}
