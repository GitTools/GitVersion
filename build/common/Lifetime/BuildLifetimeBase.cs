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
        var gitversionTool = context.GetDogFoodGitVersionToolLocation();
        var gitVersionSettings = new GitVersionSettings
        {
            OutputTypes = new HashSet<GitVersionOutput> { GitVersionOutput.Json, GitVersionOutput.BuildServer },
            ToolPath = context.Tools.Resolve(new[] { "dotnet.exe", "dotnet" }),
            ArgumentCustomization = args => args.Prepend(gitversionTool!.FullPath)
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

            context.Information("Pull Request:      {0}", context.IsPullRequest);
            context.Information("Original Repo:     {0}", context.IsOriginalRepo);
            context.Information("Branch Name:       {0}", context.BranchName);
            context.Information("Main Branch:       {0}", context.IsMainBranch);
            context.Information("Support Branch:    {0}", context.IsSupportBranch);
            context.Information("Tagged:            {0}", context.IsTagged);

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
            context.Information("Target:            {0}", context.Argument<string>(Arguments.Target));
        }
        context.Information("Version:           {0}", context.Version?.SemVersion);
        context.Information("Build Agent:       {0}", context.GetBuildAgent());
        context.Information("OS:                {0}", context.GetOS());
        context.Information("Pull Request:      {0}", context.IsPullRequest);
        context.Information("Original Repo:     {0}", context.IsOriginalRepo);
        context.Information("Branch Name:       {0}", context.BranchName);
        context.Information("Main Branch:       {0}", context.IsMainBranch);
        context.Information("Support Branch:    {0}", context.IsSupportBranch);
        context.Information("Tagged:            {0}", context.IsTagged);
    }
}
