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

        var portableNuspecFile = Paths.Nuspec.CombineWithFilePath("GitVersion.Portable.nuspec");
        if (!context.FileExists(portableNuspecFile))
            return;

        var artifactPath = context.MakeAbsolute(Paths.ArtifactsBinPortable).FullPath;

        var chocolateySettings = new ChocolateyPackSettings
        {
            LimitOutput = true,
            Version = context.Version?.SemVersion,
            OutputDirectory = Paths.Nuget,
            Files = context.GetFiles(artifactPath + "/**/*.*")
                .Select(file => new ChocolateyNuSpecContent { Source = file.FullPath, Target = file.FullPath.Replace(artifactPath, "") })
                .ToArray()
        };
        context.ChocolateyPack(portableNuspecFile, chocolateySettings);
    }
}
