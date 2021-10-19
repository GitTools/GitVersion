namespace Common.Utilities;

public class BuildContextBase : FrostingContext
{
    protected BuildContextBase(ICakeContext context) : base(context)
    {
    }
    public BuildVersion? Version { get; set; }

    public bool IsOriginalRepo { get; set; }
    public bool IsMainBranch { get; set; }
    public bool IsPullRequest { get; set; }
    public bool IsTagged { get; set; }
    public bool IsLocalBuild { get; set; }
    public bool IsAzurePipelineBuild { get; set; }
    public bool IsGitHubActionsBuild { get; set; }
    public bool IsOnWindows { get; set; }
    public bool IsOnLinux { get; set; }
    public bool IsOnMacOS { get; set; }
    public bool IsOnMainBranchOriginalRepo => !IsLocalBuild && IsOriginalRepo && IsMainBranch && !IsPullRequest;
    public bool IsStableRelease => IsOnMainBranchOriginalRepo && IsTagged;
    public bool IsPreRelease => IsOnMainBranchOriginalRepo && !IsTagged;
}
