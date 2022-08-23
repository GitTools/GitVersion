using GitVersion.VersionCalculation;

namespace GitVersion;

public interface IIgnoredFilterProvider
{
    IVersionFilter[] Provide();
}
