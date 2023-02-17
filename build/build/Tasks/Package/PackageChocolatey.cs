using Cake.Common.Tools.Chocolatey;
using Cake.Common.Tools.Chocolatey.Pack;
using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(PackageChocolatey))]
[TaskDescription("Creates the chocolatey packages")]
[IsDependentOn(typeof(PackagePrepare))]
public class PackageChocolatey : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsOnWindows, $"{nameof(PackageChocolatey)} works only on Windows agents.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        context.EnsureDirectoryExists(Paths.Nuget);

        var artifactPath = context.MakeAbsolute(Paths.ArtifactsBinPortable).FullPath;

        var portableSettings = GetChocolateyPackSettings(context, "GitVersion.Portable");
        portableSettings.Files = context.GetFiles(artifactPath + "/**/*.*")
            .Select(file => new ChocolateyNuSpecContent { Source = file.FullPath, Target = file.FullPath.Replace(artifactPath, "") })
            .ToArray();

        context.ChocolateyPack(portableSettings);

        var metaPackageSettings = GetChocolateyPackSettings(context, "GitVersion");
        metaPackageSettings.Files = context.GetFiles(artifactPath + "/**/*.txt")
            .Select(file => new ChocolateyNuSpecContent { Source = file.FullPath, Target = file.FullPath.Replace(artifactPath, "") })
            .ToArray();

        metaPackageSettings.Dependencies = new[]
        {
            new ChocolateyNuSpecDependency { Id = "GitVersion.Portable", Version = context.Version?.ChocolateyVersion }
        };

        context.ChocolateyPack(metaPackageSettings);
    }

    private static ChocolateyPackSettings GetChocolateyPackSettings(BuildContextBase context, string id)
    {
        var chocolateySettings = new ChocolateyPackSettings
        {
            Id = id,
            Version = context.Version?.ChocolateyVersion,
            Title = "GitVersion",
            Description = "Derives SemVer information from a repository following GitFlow or GitHubFlow.",
            Authors = new[] { "GitTools and Contributors" },
            Owners = new[] { "GitTools and Contributors" },
            Copyright = $"Copyright GitTools {DateTime.Now.Year}",
            DocsUrl = new Uri("https://gitversion.net/docs/"),
            LicenseUrl = new Uri("https://opensource.org/license/mit/"),
            ProjectUrl = new Uri("https://github.com/GitTools/GitVersion"),
            ProjectSourceUrl = new Uri("https://github.com/GitTools/GitVersion"),
            IconUrl = new Uri("https://raw.githubusercontent.com/GitTools/graphics/master/GitVersion/Color/icon_100x100.png"),
            RequireLicenseAcceptance = false,
            Tags = new[] { "Git", "Versioning", "GitVersion", "GitFlowVersion", "GitFlow", "GitHubFlow", "SemVer" },
            ReleaseNotes = new[] { $"https://github.com/GitTools/GitVersion/releases/tag/{context.Version?.ChocolateyVersion}" },
            OutputDirectory = Paths.Nuget,
            LimitOutput = true,
        };
        return chocolateySettings;
    }
}
