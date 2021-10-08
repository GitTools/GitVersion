using Cake.Incubator.LoggingExtensions;
using Common.Addins.GitVersion;

namespace Common.Utilities;

public class BuildLifetimeBase<T> : FrostingLifetime<T> where T : BuildContextBase
{
    public override void Setup(T context)
    {
        var buildSystem = context.BuildSystem();
        context.IsLocalBuild = buildSystem.IsLocalBuild;
        context.IsAzurePipelineBuild = buildSystem.IsRunningOnAzurePipelines || buildSystem.IsRunningOnAzurePipelinesHosted;
        context.IsGitHubActionsBuild = buildSystem.IsRunningOnGitHubActions;

        context.IsPullRequest = buildSystem.IsPullRequest;
        context.IsOriginalRepo = context.IsOriginalRepo();
        context.IsMainBranch = context.IsMainBranch();
        context.IsTagged = context.IsTagged();

        context.IsOnWindows = context.IsRunningOnWindows();
        context.IsOnLinux = context.IsRunningOnLinux();
        context.IsOnMacOS = context.IsRunningOnMacOs();

        var gitVersion = context.GitVersion(new GitVersionSettings
        {
            OutputTypes = new HashSet<GitVersionOutput> { GitVersionOutput.Json, GitVersionOutput.BuildServer }
        });

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
            context.Information("Main Branch:       {0}", context.IsMainBranch);
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
        context.Information("Main Branch:       {0}", context.IsMainBranch);
        context.Information("Tagged:            {0}", context.IsTagged);
    }
}
