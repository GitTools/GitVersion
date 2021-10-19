using Common.Addins.GitVersion;
using Common.Utilities;
using Xunit;

namespace Artifacts.Tasks;

[TaskName(nameof(ArtifactsExecutableTest))]
[TaskDescription("Tests the cmdline and portable packages on windows")]
public class ArtifactsExecutableTest : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsOnWindows, $"{nameof(ArtifactsExecutableTest)} works only on Windows agents.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        var packagesToTest = new[]
        {
            "GitVersion.Commandline", "GitVersion.Portable"
        };
        foreach (var packageToTest in packagesToTest)
        {
            PackageTest(context, packageToTest);
        }
    }
    private static void PackageTest(BuildContextBase context, string packageToTest)
    {
        if (context.Version == null)
            return;

        var outputDirectory = Paths.Packages.Combine("test");

        context.NuGetInstall(packageToTest, new NuGetInstallSettings
        {
            Source = new[]
            {
                context.MakeAbsolute(Paths.Nuget).FullPath
            },
            ExcludeVersion = true,
            Prerelease = true,
            OutputDirectory = outputDirectory
        });

        var settings = new GitVersionSettings
        {
            OutputTypes = new HashSet<GitVersionOutput>
            {
                GitVersionOutput.Json
            },
            ToolPath = outputDirectory.Combine(packageToTest).Combine("tools").CombineWithFilePath("gitversion.exe").FullPath
        };
        var gitVersion = context.GitVersion(settings);

        Assert.Equal(context.Version.GitVersion.FullSemVer, gitVersion.FullSemVer);
    }
}
