namespace GitVersion.Git;

/// <summary>Represents a Git refspec that maps source references to destination references for a remote operation.</summary>
public interface IRefSpec : IEquatable<IRefSpec?>, IComparable<IRefSpec>
{
    /// <summary>Gets the full refspec string (e.g. <c>+refs/heads/*:refs/remotes/origin/*</c>).</summary>
    string Specification { get; }

    /// <summary>Gets the direction of this refspec (fetch or push).</summary>
    RefSpecDirection Direction { get; }

    /// <summary>Gets the source pattern of the refspec.</summary>
    string Source { get; }

    /// <summary>Gets the destination pattern of the refspec.</summary>
    string Destination { get; }
}
