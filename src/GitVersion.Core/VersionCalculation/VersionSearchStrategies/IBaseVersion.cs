namespace GitVersion.VersionCalculation;

/// <summary>Represents a resolved base version that carries both a semantic version value and a pending increment.</summary>
public interface IBaseVersion : IBaseVersionIncrement
{
    /// <summary>Gets the discovered base semantic version.</summary>
    SemanticVersion SemanticVersion { get; }

    /// <summary>Gets the version field that will be incremented to produce the next version.</summary>
    VersionField Increment { get; }
}
