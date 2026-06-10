using GitVersion.Git;

namespace GitVersion.VersionCalculation;

/// <summary>Base interface for types that describe a version increment: a source description and the commit that anchors the calculation.</summary>
public interface IBaseVersionIncrement
{
    /// <summary>Gets a human-readable description of the strategy or artifact that produced this increment.</summary>
    string Source { get; }

    /// <summary>Gets the commit that the base version was derived from, or <see langword="null"/> when the version has an external source.</summary>
    ICommit? BaseVersionSource { get; }
}
