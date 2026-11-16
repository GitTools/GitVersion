using Cake.Incubator.LoggingExtensions;
using Common.Addins.GitVersion;
using Common.Utilities;

namespace Common.Lifetime;

public class BuildLifetimeBase<T> : FrostingLifetime<T> where T : BuildContextBase
{
    public override void Setup(T context, ISetupContext info)
    {
        var buildSystem = context.BuildSystem();
        var isDebug = context.IsEnabled(EnvVars.ActionsRunnerDebug, false);
        var isStepDebug = context.IsEnabled(EnvVars.ActionsStepDebug, false);
        var isDiagnosticsEnabled = context.IsEnabled(EnvVars.EnabledDiagnostics, false);
        if (isDebug || isStepDebug || isDiagnosticsEnabled)
        {
            context.Log.Verbosity = Verbosity.Diagnostic;
        }

        context.IsLocalBuild = buildSystem.IsLocalBuild;
        context.IsAzurePipelineBuild = buildSystem.IsRunningOnAzurePipelines;
        context.IsGitHubActionsBuild = buildSystem.IsRunningOnGitHubActions;

        context.IsPullRequest = buildSystem.IsPullRequest;
        context.BranchName = context.GetBranchName();
        context.RepositoryName = context.GetRepositoryName();
        context.IsOriginalRepo = context.IsOriginalRepo();
        context.IsMainBranch = context.IsMainBranch();
        context.IsSupportBranch = context.IsSupportBranch();
        context.IsNextBranch = context.IsNextBranch();
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

        var releaseTag = context.EnvironmentVariable("GITVERSION_RELEASE_TAG");
        var publication = string.IsNullOrEmpty(releaseTag)
            ? null
            : ReleasePublication.Create(releaseTag, context.ExecuteCommand("git", "rev-parse HEAD").Single());
        if (publication is not null)
        {
            // Pin the local calculation branch as well as HEAD. --commit uses
            // history timestamps, so a later tip with the same timestamp must
            // not become the current commit. Remote refs remain unchanged.
            context.ExecuteCommand("git", $"update-ref refs/heads/{Constants.DefaultBranch} {publication.TargetCommitish}");
        }
        var gitVersionSettings = new GitVersionSettings
        {
            OutputTypes = [GitVersionOutput.Json, GitVersionOutput.BuildServer],
            ToolPath = context.Tools.Resolve(["dotnet.exe", "dotnet"]),
            ArgumentCustomization = args =>
            {
                args.Prepend(gitVersionPath.FullPath);
                if (publication is not null)
                {
                    args.Append("--no-normalize").Append("--commit").AppendQuoted(publication.TargetCommitish);
                    if (publication.PrereleaseLabel is not null)
                    {
                        args.Append("--override-config").AppendQuoted($"calculation.branches.main.label={publication.PrereleaseLabel}");
                    }
                }
                return args;
            }
        };
        if (!string.IsNullOrEmpty(releaseTag))
        {
            // Keep main's configuration while calculating the checked-out tag.
            // --no-normalize prevents a later main tip replacing this revision.
            gitVersionSettings.EnvironmentVariables = new Dictionary<string, string>
            {
                ["GITHUB_REF"] = $"refs/heads/{Constants.DefaultBranch}",
                ["GITHUB_REF_TYPE"] = "branch",
                ["GITHUB_SHA"] = publication!.TargetCommitish
            };
        }

        var gitVersion = context.GitVersion(gitVersionSettings);

        context.Version = BuildVersion.Calculate(gitVersion);

        if (!string.IsNullOrEmpty(releaseTag) && context.Version.SemVersion != releaseTag)
        {
            throw new InvalidOperationException($"Calculated version {context.Version.SemVersion} does not match selected release tag {releaseTag}.");
        }
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
    protected static void LogBuildInformation(T context)
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
        context.Information($"Next Branch:          {context.IsNextBranch}");
        context.Information($"Tagged:               {context.IsTagged}");
        context.Information($"IsTaggedRelease:      {context.IsTaggedRelease}");
        context.Information($"IsTaggedPreRelease:   {context.IsTaggedPreRelease}");
        context.Information($"IsInternalPreRelease: {context.IsInternalPreRelease}");
    }
}
