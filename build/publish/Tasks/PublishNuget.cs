using System;
using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.NuGet.Push;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.List;
using Cake.Frosting;
using Common.Utilities;

namespace Publish.Tasks
{
    [TaskName(nameof(PublishNuget))]
    [TaskDescription("Publish nuget packages")]
    public class PublishNuget : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            var shouldRun = true;
            shouldRun &= context.ShouldRun(context.IsPreRelease || context.IsStableRelease, $"{nameof(PublishNuget)} works only for releases.");

            return shouldRun;
        }

        public override void Run(BuildContext context)
        {
            // publish to github packages for commits on main and on original repo
            if (context.IsGitHubActionsBuild && context.IsOnMainBranchOriginalRepo)
            {
                var apiKey = context.Credentials?.GitHub?.Token;
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
        private static void PublishToNugetRepo(BuildContext context, string? apiKey, string? apiUrl)
        {
            var nugetVersion = context.Version!.NugetVersion;
            foreach (var (packageName, filePath, _) in context.Packages.Where(x => !x.IsChocoPackage))
            {
                context.Information($"Package {packageName}, version {nugetVersion} is being published.");
                context.DotNetCoreNuGetPush(filePath.FullPath, new DotNetCoreNuGetPushSettings
                {
                    ApiKey = apiKey, Source = apiUrl
                });
            }
        }
        // TODO check package is already published
        private static bool IsPackagePublished(BuildContext context, string? packageName, string? nugetVersion)
        {
            var apiUrl = context.Credentials?.Nuget?.ApiUrl;
            var publishedPackages = context.NuGetList($"packageId:{packageName} version:{nugetVersion}", new NuGetListSettings
            {
                Source = new[]
                {
                    apiUrl
                },
                AllVersions = true,
            });

            return publishedPackages.Any();
        }
    }
}
