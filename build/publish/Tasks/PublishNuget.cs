using Cake.Common.Tools.DotNetCore.NuGet.Push;
using Common.Utilities;

namespace Publish.Tasks;

[TaskName(nameof(PublishNuget))]
[TaskDescription("Publish nuget packages")]
[IsDependentOn(typeof(PublishNugetInternal))]
public class PublishNuget : FrostingTask<BuildContext>
{
}

[TaskName(nameof(PublishNugetInternal))]
[TaskDescription("Publish nuget packages")]
public class PublishNugetInternal : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsGitHubActionsBuild, $"{nameof(PublishNuget)} works only on GitHub Actions.");
        shouldRun &= context.ShouldRun(context.IsPreRelease || context.IsStableRelease, $"{nameof(PublishNuget)} works only for releases.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        // publish to github packages for commits on main and on original repo
        if (context.IsGitHubActionsBuild && context.IsOnMainBranchOriginalRepo)
        {
            var apiKey = context.Credentials?.GitHub?.Token;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Could not resolve NuGet GitHub Packages API key.");
            }
            PublishToNugetRepo(context, apiKey, Constants.GithubPackagesUrl);
        }
        // publish to nuget.org for stable releases
        if (context.IsStableRelease)
        {
            var apiKey = context.Credentials?.Nuget?.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Could not resolve NuGet org API key.");
            }

            PublishToNugetRepo(context, apiKey, Constants.NugetOrgUrl);
        }
    }
    private static void PublishToNugetRepo(BuildContext context, string apiKey, string apiUrl)
    {
        var nugetVersion = context.Version!.NugetVersion;
        foreach (var (packageName, filePath, _) in context.Packages.Where(x => !x.IsChocoPackage))
        {
            context.Information($"Package {packageName}, version {nugetVersion} is being published.");
            context.DotNetCoreNuGetPush(filePath.FullPath, new DotNetCoreNuGetPushSettings
            {
                ApiKey = apiKey,
                Source = apiUrl,
                SkipDuplicate = true
            });
        }
    }
}
