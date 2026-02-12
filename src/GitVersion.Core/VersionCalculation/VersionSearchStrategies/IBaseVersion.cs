namespace GitVersion.VersionCalculation;

public interface IBaseVersion : IBaseVersionIncrement
{
    SemanticVersion SemanticVersion { get; }

    VersionField Increment { get; }
}
