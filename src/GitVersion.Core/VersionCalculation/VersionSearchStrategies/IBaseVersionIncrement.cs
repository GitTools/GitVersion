using GitVersion.Git;

namespace GitVersion.VersionCalculation;

public interface IBaseVersionIncrement
{
    string Source { get; }

    VersionIncrementSourceType SourceType { get; }

    ICommit? BaseVersionSource { get; }
}
