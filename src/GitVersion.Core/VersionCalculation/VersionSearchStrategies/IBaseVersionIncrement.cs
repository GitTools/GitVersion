namespace GitVersion.VersionCalculation;

public interface IBaseVersionIncrement
{
    string Source { get; }

    ICommit? BaseVersionSource { get; }
}
