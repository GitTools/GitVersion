using System;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Core;
using Cake.Frosting;
using Common.Utilities;

namespace Build
{
    public class BuildContext : FrostingContext
    {
        public new string Configuration { get; set; }

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

        public Paths Paths { get; } = new();
        public DotNetCoreMSBuildSettings MSBuildSettings { get; set; } = new();
        public BuildVersion Version { get; set; }
        public BuildContext(ICakeContext context)
            : base(context)
        {
            // Configuration = context.Arguments.GetArgument("configuration");
            // Version = BuildVersion.Calculate(context);
            // SetMSBuildSettingsVersion(MSBuildSettings, Version);
        }

        private void SetMSBuildSettingsVersion(DotNetCoreMSBuildSettings msBuildSettings, BuildVersion version)
        {
            msBuildSettings.WithProperty("Version", version.SemVersion);
            msBuildSettings.WithProperty("AssemblyVersion", version.Version);
            msBuildSettings.WithProperty("PackageVersion", version.NugetVersion);
            msBuildSettings.WithProperty("FileVersion", version.Version);
            msBuildSettings.WithProperty("InformationalVersion", version.GitVersion.InformationalVersion);
            msBuildSettings.WithProperty("RepositoryBranch", version.GitVersion.BranchName);
            msBuildSettings.WithProperty("RepositoryCommit", version.GitVersion.Sha);
            msBuildSettings.WithProperty("NoPackageAnalysis", "true");
        }

    }

    public class Paths
    {
        public string Artifacts { get; } = "./artifacts";
        public string Src { get; } = "./src";
        public string Build { get; } = "./build";
    }
}
