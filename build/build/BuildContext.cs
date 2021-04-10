using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Core;
using Cake.Frosting;
using Common.Utilities;

namespace Build
{
    public class BuildContext : FrostingContext
    {
        public string BuildConfiguration { get; set; } = "Release";

        public bool IsOriginalRepo { get; set; }
        public bool IsMainBranch { get; set; }
        public bool IsPullRequest { get; set; }
        public bool IsTagged { get; set; }

        public bool IsLocalBuild { get; set; }
        public bool IsAppVeyorBuild { get; set; }
        public bool IsAzurePipelineBuild { get; set; }
        public bool IsGitHubActionsBuild { get; set; }

        public bool IsOnWindows { get; set; }
        public bool IsOnLinux { get; set; }
        public bool IsOnMacOS { get; set; }

        public bool EnabledUnitTests { get; set; }

        public BuildVersion? Version { get; set; }

        public DotNetCoreMSBuildSettings MsBuildSettings { get; } = new();

        public BuildContext(ICakeContext context) : base(context)
        {
        }
    }

    public class Paths
    {
        public static string Artifacts => "./artifacts";
        public static string Src => "./src";
        public static string Build => "./build";
        public static string TestOutput => $"{Artifacts}/test-results";
    }
}
