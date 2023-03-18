using Cake.Common.Tools.Chocolatey;
using Cake.Common.Tools.Chocolatey.Push;
using Common.Utilities;

namespace Publish.Tasks;

[TaskName(nameof(PublishChocolatey))]
[TaskDescription("Publish chocolatey packages")]
[IsDependentOn(typeof(PublishChocolateyInternal))]
public class PublishChocolatey : FrostingTask<BuildContext>
{
}

[TaskName(nameof(PublishChocolateyInternal))]
[TaskDescription("Publish chocolatey packages")]
public class PublishChocolateyInternal : AsyncFrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsGitHubActionsBuild, $"{nameof(PublishChocolatey)} works only on GitHub Actions.");
        shouldRun &= context.ShouldRun(context.IsOnWindows, $"{nameof(PublishChocolatey)} works only on windows.");
        shouldRun &= context.ShouldRun(context.IsStableRelease || context.IsTaggedPreRelease, $"{nameof(PublishChocolatey)} works only for tagged releases.");

        return shouldRun;
    }

    public override async Task RunAsync(BuildContext context)
    {
        var apiKey = context.Credentials?.Chocolatey?.ApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Could not resolve Chocolatey API key.");
        }

        var nugetVersion = context.Version!.NugetVersion;
        var packages = context.Packages
            .Where(x => x.IsChocoPackage)
            .OrderByDescending(x => x.PackageName);
        foreach (var (packageName, filePath, _) in packages)
        {
            if (IsPackagePublished(context, packageName, nugetVersion)) continue;
            try
            {
                context.Information($"Package {packageName}, version {nugetVersion} is being published.");
                context.ChocolateyPush(filePath.FullPath, new ChocolateyPushSettings
                {
                    ApiKey = apiKey,
                    Source = Constants.ChocolateyUrl,
                    Force = true
                });
            }
            catch (Exception)
            {
                context.Warning($"There is an exception publishing the Package {packageName}.");
                // chocolatey sometimes fails with an error, even if the package gets pushed
            }

            // wait 5 seconds to avoid the meta-package to be published before the other packages
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    private static bool IsPackagePublished(ICakeContext context, string packageName, string? nugetVersion)
    {
        var chocoExe = context.Tools.Resolve("choco.exe");
        var chocoListOutput = context.ExecuteCommand(chocoExe, $"list {packageName} --version={nugetVersion}");
        return chocoListOutput.Contains("1 packages found.");
    }
}
