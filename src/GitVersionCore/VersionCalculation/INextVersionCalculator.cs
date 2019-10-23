using GitVersion.Configuration;
using GitVersion.SemanticVersioning;

namespace GitVersion.VersionCalculation
{
    public interface INextVersionCalculator
    {
        SemanticVersion FindVersion(GitVersionContext context);
        string GetBranchSpecificTag(EffectiveConfiguration configuration, string branchFriendlyName, string branchNameOverride);
    }
}