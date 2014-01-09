namespace GitFlowVersion
{
    using System.Collections.Generic;
    using VersionBuilders;

    public static class BuildOutputGenerator
    {
        public static IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch, IBuildServer buildServer)
        {
            var semanticVersion = versionAndBranch.Version;
            var releaseInfo = versionAndBranch.CalculateReleaseInfo();
            yield return buildServer.GenerateSetParameterMessage("Major", semanticVersion.Major.ToString());
            yield return buildServer.GenerateSetParameterMessage("Minor", semanticVersion.Minor.ToString());
            yield return buildServer.GenerateSetParameterMessage("Patch", semanticVersion.Patch.ToString());
            yield return buildServer.GenerateSetParameterMessage("Stability", releaseInfo.Stability.ToString());
            yield return buildServer.GenerateSetParameterMessage("PreReleaseNumber", releaseInfo.ReleaseNumber.ToString());
            yield return buildServer.GenerateSetParameterMessage("Version", versionAndBranch.GenerateSemVer());
            yield return buildServer.GenerateSetParameterMessage("NugetVersion", versionAndBranch.GenerateNugetVersion());
        }
    }
}
