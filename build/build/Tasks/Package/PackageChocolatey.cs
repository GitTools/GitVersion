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
        portableSettings.Files = [.. context.GetFiles(artifactPath + "/**/*.*").Select(file => new ChocolateyNuSpecContent { Source = file.FullPath, Target = file.FullPath.Replace(artifactPath, "") })];

        context.ChocolateyPack(portableSettings);

        var metaPackageSettings = GetChocolateyPackSettings(context, "GitVersion");
        metaPackageSettings.Files = [.. context.GetFiles(artifactPath + "/**/*.txt").Select(file => new ChocolateyNuSpecContent { Source = file.FullPath, Target = file.FullPath.Replace(artifactPath, "") })];

        metaPackageSettings.Dependencies =
        [
            new ChocolateyNuSpecDependency { Id = "GitVersion.Portable", Version = context.Version?.ChocolateyVersion }
        ];

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
            Authors = ["GitTools and Contributors"],
            Owners = ["GitTools and Contributors"],
            Copyright = $"Copyright GitTools {DateTime.Now.Year}",
            DocsUrl = new Uri("https://gitversion.net/docs/"), // NOSONAR: Canonical documentation URL in package metadata.
            LicenseUrl = new Uri("https://opensource.org/license/mit/"), // NOSONAR: Canonical MIT license URL in package metadata.
            ProjectUrl = new Uri("https://github.com/GitTools/GitVersion"), // NOSONAR: Canonical project URL in package metadata.
            ProjectSourceUrl = new Uri("https://github.com/GitTools/GitVersion"), // NOSONAR: Canonical source repository URL in package metadata.
            IconUrl = new Uri("https://raw.githubusercontent.com/GitTools/graphics/master/GitVersion/Color/icon_100x100.png"), // NOSONAR: Published project icon URL in package metadata.
            RequireLicenseAcceptance = false,
            Tags = ["Git", "Versioning", "GitVersion", "GitFlowVersion", "GitFlow", "GitHubFlow", "SemVer"],
            ReleaseNotes = [$"https://github.com/GitTools/GitVersion/releases/tag/{context.Version?.ChocolateyVersion}"
            ],
            OutputDirectory = Paths.Nuget,
            LimitOutput = true,
        };
        return chocolateySettings;
    }
}
