using GitVersion.Configuration;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.VersionCalculation;

public class TestBaseVersionCalculator : IBaseVersionCalculator
{
    private readonly SemanticVersion semanticVersion;
    private readonly SemanticVersion incrementedVersion;
    private readonly bool shouldIncrement;
    private readonly ICommit source;

    public TestBaseVersionCalculator(bool shouldIncrement, SemanticVersion semanticVersion, ICommit source)
    {
        this.semanticVersion = semanticVersion;
        this.source = source;
        this.shouldIncrement = shouldIncrement;
        incrementedVersion = shouldIncrement ? semanticVersion.IncrementVersion(VersionField.Patch) : semanticVersion;
    }

    public NextVersion Calculate(IBranch branch, Config configuration)
    {
        var baseVersion = new BaseVersion("Test source", shouldIncrement, semanticVersion, source, null);
        var fallbackBranchConfiguration = configuration.GetFallbackBranchConfiguration();
        var effectiveConfiguration = new EffectiveConfiguration(configuration, fallbackBranchConfiguration);
        return new(incrementedVersion, baseVersion, branch, effectiveConfiguration);
    }
}
