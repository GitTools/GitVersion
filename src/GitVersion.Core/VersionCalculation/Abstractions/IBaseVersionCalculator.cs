using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation;

public interface IBaseVersionCalculator
{
    (BaseVersion, EffectiveBranchConfiguration) GetBaseVersion();
}
