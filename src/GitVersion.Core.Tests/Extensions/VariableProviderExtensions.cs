using GitVersion.Model.Configuration;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.Extensions
{
    internal static class VariableProviderExtensions
    {
        public static VersionVariables GetVariablesFor(this IVariableProvider variableProvider,
            SemanticVersion semanticVersion, EffectiveConfiguration config, bool isCurrentCommitTagged)
        {
            var commitMock = GitToolsTestingExtensions.CreateMockCommit();
            var branchMock = GitToolsTestingExtensions.CreateMockBranch("develop", commitMock);
            var baseVersion = new BaseVersion("dummy", false, semanticVersion, commitMock, string.Empty);
            var nextVersion = new NextVersion(semanticVersion, baseVersion, new(branchMock, config));

            return variableProvider.GetVariablesFor(nextVersion, isCurrentCommitTagged);
        }
    }
}
