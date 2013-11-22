namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;
    using GitFlowVersion.VersionBuilders;

    static class IntegrationHelper
    {
        public static IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch, IVersionBuilder versionBuilder, 
            Func<string, string, string> generateBuildParameter)
        {
            var semanticVersion = versionAndBranch.Version;

            yield return versionBuilder.GenerateBuildVersion(versionAndBranch);
            yield return generateBuildParameter("Major", semanticVersion.Major.ToString());
            yield return generateBuildParameter("Minor", semanticVersion.Minor.ToString());
            yield return generateBuildParameter("Patch", semanticVersion.Patch.ToString());
            yield return generateBuildParameter("Stability", semanticVersion.Stability.ToString());
            yield return generateBuildParameter("PreReleaseNumber", semanticVersion.PreReleasePartOne.ToString());
            yield return generateBuildParameter("Version", versionBuilder.CreateVersionString(versionAndBranch));
            yield return generateBuildParameter("NugetVersion", NugetVersionBuilder.GenerateNugetVersion(versionAndBranch));
        }
    }
}
