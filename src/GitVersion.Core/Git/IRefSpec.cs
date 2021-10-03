namespace GitVersion;

public interface IRefSpec : IEquatable<IRefSpec?>, IComparable<IRefSpec>
{
    string Specification { get; }
    RefSpecDirection Direction { get; }
    string Source { get; }
    string Destination { get; }
}
