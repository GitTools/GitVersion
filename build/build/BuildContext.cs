using System.Collections.Generic;
using Build.Utilities;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;
using Common.Utilities;

namespace Build
{
    public class BuildContext : FrostingContext
    {
        public string MsBuildConfiguration { get; set; } = "Release";

        public readonly Dictionary<PlatformFamily, string[]> NativeRuntimes = new()
        {
            [PlatformFamily.Windows] = new[] { "win-x64", "win-x86" },
            [PlatformFamily.Linux] = new[] { "linux-x64", "linux-musl-x64" },
            [PlatformFamily.OSX] = new[] { "osx-x64" },
        };
        public readonly Dictionary<string, DirectoryPath> PackagesBuildMap = new()
        {
            ["GitVersion.CommandLine"] = Paths.ArtifactsBinCmdline,
            ["GitVersion.Portable"] = Paths.ArtifactsBinPortable,
        };
        public BuildPackages? Packages { get; set; }

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

        public bool IsOnMainBranchOriginalRepo => !IsLocalBuild && IsOriginalRepo && IsMainBranch && !IsPullRequest;
        public bool IsStableRelease => IsOnMainBranchOriginalRepo && IsTagged;
        public bool IsPreRelease => IsOnMainBranchOriginalRepo && !IsTagged;

        public bool EnabledUnitTests { get; set; }

        public BuildVersion? Version { get; set; }
        public BuildCredentials? Credentials { get; set; }

        public DotNetCoreMSBuildSettings MsBuildSettings { get; } = new();

        public BuildContext(ICakeContext context) : base(context)
        {
        }
    }
}
