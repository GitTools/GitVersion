using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.VersionCalculation;

public class TestBaseVersionCalculator : IBaseVersionCalculator
{
    private readonly SemanticVersion semanticVersion;
    private readonly bool shouldIncrement;
    private readonly ICommit source;
    private readonly EffectiveBranchConfiguration configuration;

    public TestBaseVersionCalculator(bool shouldIncrement, SemanticVersion semanticVersion, ICommit source, EffectiveBranchConfiguration configuration)
    {
        this.semanticVersion = semanticVersion;
        this.source = source;
        this.shouldIncrement = shouldIncrement;
        this.configuration = configuration;
    }

    public (BaseVersion, EffectiveBranchConfiguration) GetBaseVersion() => new(new("Test source", this.shouldIncrement, this.semanticVersion, this.source, null), this.configuration);
}
