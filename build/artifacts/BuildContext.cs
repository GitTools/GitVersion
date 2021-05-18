using System.Collections.Generic;
using Artifacts.Utilities;
using Cake.Core;
using Cake.Frosting;
using Common.Utilities;

namespace Artifacts
{
    public record DockerImage(string Distro, string TargetFramework);

    public class BuildContext : FrostingContext
    {
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
        public bool IsDockerOnLinux { get; set; }

        public bool IsOnMainBranchOriginalRepo => !IsLocalBuild && IsOriginalRepo && IsMainBranch && !IsPullRequest;
        public bool IsStableRelease => IsOnMainBranchOriginalRepo && IsTagged;
        public bool IsPreRelease => IsOnMainBranchOriginalRepo && !IsTagged;

        public BuildVersion? Version { get; set; }
        public BuildCredentials? Credentials { get; set; }
        public IEnumerable<DockerImage> Images { get; set; } = new List<DockerImage>();


        public BuildContext(ICakeContext context) : base(context)
        {
        }
    }
}
