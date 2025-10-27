using Cake.Incubator.LoggingExtensions;
using Common.Addins.GitVersion;
using Common.Utilities;

namespace Common.Lifetime;

public class BuildLifetimeBase<T> : FrostingLifetime<T> where T : BuildContextBase
{
    public override void Setup(T context, ISetupContext info)
    {
        var buildSystem = context.BuildSystem();
        context.IsLocalBuild = buildSystem.IsLocalBuild;
        context.IsAzurePipelineBuild = buildSystem.IsRunningOnAzurePipelines;
        context.IsGitHubActionsBuild = buildSystem.IsRunningOnGitHubActions;

        context.IsPullRequest = buildSystem.IsPullRequest;
        context.BranchName = context.GetBranchName();
        context.RepositoryName = context.GetRepositoryName();
        context.IsOriginalRepo = context.IsOriginalRepo();
        context.IsMainBranch = context.IsMainBranch();
        context.IsSupportBranch = context.IsSupportBranch();
        context.IsTagged = context.IsTagged();

        context.IsOnWindows = context.IsRunningOnWindows();
        context.IsOnLinux = context.IsRunningOnLinux();
        context.IsOnMacOS = context.IsRunningOnMacOs();

        if (info.TargetTask.Name == "BuildPrepare")
        {
            context.Information("Running BuildPrepare...");
            return;
        }

        var gitVersionPath = context.GetGitVersionDotnetToolLocation();
        if (gitVersionPath is null || !context.FileExists(gitVersionPath))
        {
            throw new FileNotFoundException("Failed to locate the Release build of gitversion.dll in ./tools/gitversion. Try running \"./build.ps1 -Stage build -Target BuildPrepare\"");
        }

        var gitVersionSettings = new GitVersionSettings
        {
            OutputTypes = [GitVersionOutput.Json, GitVersionOutput.BuildServer],
            ToolPath = context.Tools.Resolve(["dotnet.exe", "dotnet"]),
            ArgumentCustomization = args => args.Prepend(gitVersionPath.FullPath)
        };

        var gitVersion = context.GitVersion(gitVersionSettings);

        context.Version = BuildVersion.Calculate(gitVersion);
    }
    public override void Teardown(T context, ITeardownContext info)
    {
        context.StartGroup("Build Teardown");
        try
        {
            context.Information("Starting Teardown...");

            LogBuildInformation(context);

            context.Information("Finished running tasks.");
        }
        catch (Exception exception)
        {
            context.Error(exception.Dump());
        }
        context.EndGroup();
    }
    protected void LogBuildInformation(T context)
    {
        if (context.HasArgument(Arguments.Target))
        {
            context.Information($"Target:               {context.Argument<string>(Arguments.Target)}");
        }
        if (context.Version is not null)
        {
            context.Information($"Version:              {context.Version.SemVersion}");
        }
        context.Information($"Build Agent:          {context.GetBuildAgent()}");
        context.Information($"OS:                   {context.GetOS()}");
        context.Information($"Pull Request:         {context.IsPullRequest}");
        context.Information($"Repository Name:      {context.RepositoryName}");
        context.Information($"Original Repository:  {context.IsOriginalRepo}");
        context.Information($"Branch Name:          {context.BranchName}");
        context.Information($"Main Branch:          {context.IsMainBranch}");
        context.Information($"Support Branch:       {context.IsSupportBranch}");
        context.Information($"Tagged:               {context.IsTagged}");
        context.Information($"IsStableRelease:      {context.IsStableRelease}");
        context.Information($"IsTaggedPreRelease:   {context.IsTaggedPreRelease}");
        context.Information($"IsInternalPreRelease: {context.IsInternalPreRelease}");
    }
}
