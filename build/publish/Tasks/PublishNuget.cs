using Cake.Common.Tools.DotNet.NuGet.Push;
using Common.Utilities;

namespace Publish.Tasks;

[TaskName(nameof(PublishNuget))]
[TaskDescription("Publish nuget packages")]
[IsDependentOn(typeof(PublishNugetInternal))]
public class PublishNuget : FrostingTask<BuildContext>;

[TaskName(nameof(PublishNugetInternal))]
[TaskDescription("Publish nuget packages")]
public class PublishNugetInternal : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsGitHubActionsBuild, $"{nameof(PublishNuget)} works only on GitHub Actions.");
        shouldRun &= context.ShouldRun(context.IsStableRelease || context.IsTaggedPreRelease || context.IsInternalPreRelease, $"{nameof(PublishNuget)} works only for releases.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        // publish to github packages for commits on main and on original repo
        if (context.IsInternalPreRelease)
        {
            context.StartGroup("Publishing to GitHub Packages");
            var apiKey = context.Credentials?.GitHub?.Token;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Could not resolve NuGet GitHub Packages API key.");
            }
            PublishToNugetRepo(context, apiKey, Constants.GithubPackagesUrl);
            context.EndGroup();
        }
        // publish to nuget.org for tagged releases
        if (context.IsStableRelease || context.IsTaggedPreRelease)
        {
            context.StartGroup("Publishing to Nuget.org");
            var apiKey = context.Credentials?.Nuget?.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Could not resolve NuGet org API key.");
            }
            PublishToNugetRepo(context, apiKey, Constants.NugetOrgUrl);
            context.EndGroup();
        }
    }
    private static void PublishToNugetRepo(BuildContext context, string apiKey, string apiUrl)
    {
        ArgumentNullException.ThrowIfNull(context.Version);
        var nugetVersion = context.Version.NugetVersion;
        foreach (var (packageName, filePath, _) in context.Packages.Where(x => !x.IsChocoPackage))
        {
            context.Information($"Package {packageName}, version {nugetVersion} is being published.");
            context.DotNetNuGetPush(filePath.FullPath, new DotNetNuGetPushSettings
            {
                ApiKey = apiKey,
                Source = apiUrl,
                SkipDuplicate = true
            });
        }
    }
}
