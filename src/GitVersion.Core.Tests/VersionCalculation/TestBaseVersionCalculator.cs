using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.VersionCalculation;

public class TestBaseVersionCalculator : IBaseVersionCalculator
{
    private readonly SemanticVersion semanticVersion;
    private readonly bool shouldIncrement;
    private readonly ICommit source;

    public TestBaseVersionCalculator(bool shouldIncrement, SemanticVersion semanticVersion, ICommit source)
    {
        this.semanticVersion = semanticVersion;
        this.source = source;
        this.shouldIncrement = shouldIncrement;
    }

    public BaseVersion GetBaseVersion() => new("Test source", this.shouldIncrement, this.semanticVersion, this.source, null);
}
