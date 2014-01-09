namespace GitFlowVersion
{
    using System.Collections.Generic;
    using VersionBuilders;

    public static class BuildOutputGenerator
    {
        public static IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch, IBuildServer buildServer)
        {
            var semanticVersion = versionAndBranch.Version;
            yield return buildServer.GenerateSetParameterMessage("Major", semanticVersion.Major.ToString());
            yield return buildServer.GenerateSetParameterMessage("Minor", semanticVersion.Minor.ToString());
            yield return buildServer.GenerateSetParameterMessage("Patch", semanticVersion.Patch.ToString());
            yield return buildServer.GenerateSetParameterMessage("Stability", semanticVersion.Tag.InferStability().ToString());
            yield return buildServer.GenerateSetParameterMessage("PreReleaseNumber", semanticVersion.Tag.ReleaseNumber().ToString());
            yield return buildServer.GenerateSetParameterMessage("Version", versionAndBranch.GenerateSemVer());
            yield return buildServer.GenerateSetParameterMessage("NugetVersion", versionAndBranch.GenerateNugetVersion());
        }
    }
}
