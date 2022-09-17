using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation;

public interface IBaseVersionCalculator
{
    NextVersion Calculate(IBranch branch, Config configuration);
}
