using Cake.Common.Diagnostics;
using Cake.Common.Tools.GitVersion;
using Cake.Core;

namespace Common.Utilities
{
    public class BuildVersion
    {
        public GitVersion GitVersion { get; private set; }
        public string Version { get; private set; }
        public string Milestone { get; private set; }
        public string SemVersion { get; private set; }
        public string NugetVersion { get; private set; }

        public static BuildVersion Calculate(ICakeContext context)
        {
            context.Information("Calculating semantic version...");

            //     if (!context.IsLocalBuild)
            //     {
            //         // Run to set the version properties inside the CI server
            //         GitVersionRunner.Run(, GitVersionOutput.BuildServer);
            //     }
            //
            //     // Run in interactive mode to get the properties for the rest of the script
            //     var assertedversions = GitVersionRunner.Run(context, GitVersionOutput.Json);
            //
            //     var version = gitVersion.MajorMinorPatch;
            //     var semVersion = gitVersion.LegacySemVer;
            //     var nugetVersion = gitVersion.LegacySemVer;
            //
            //     if (!string.IsNullOrWhiteSpace(gitVersion.BuildMetaData))
            //     {
            //         semVersion += "-" + gitVersion.BuildMetaData;
            //         nugetVersion += "." + gitVersion.BuildMetaData;
            //     }
            //
            //     return new BuildVersion
            //     {
            //         GitVersion = gitVersion,
            //         Milestone = version,
            //         Version = version,
            //         SemVersion = semVersion,
            //         NugetVersion = nugetVersion.ToLowerInvariant(),
            //     };
            return new BuildVersion();
        }
    }
}
