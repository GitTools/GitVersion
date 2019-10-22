using GitVersion.SemanticVersioning;
using GitVersion.VersionCalculation.BaseVersionCalculators;

namespace GitVersion.VersionCalculation
{
    internal interface IMainlineVersionCalculator
    {
        SemanticVersion FindMainlineModeVersion(BaseVersion baseVersion, GitVersionContext context);
    }
}