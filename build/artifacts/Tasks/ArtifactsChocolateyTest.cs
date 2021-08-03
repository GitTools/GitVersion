using System.Collections.Generic;
using Cake.Common.IO;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Install;
using Cake.Frosting;
using Common.Addins.GitVersion;
using Common.Utilities;
using Xunit;

namespace Artifacts.Tasks
{
    [TaskName(nameof(ArtifactsChocolateyTest))]
    [TaskDescription("Tests the chocolatey package on windows")]
    public class ArtifactsChocolateyTest : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            var shouldRun = true;
            shouldRun &= context.ShouldRun(context.IsOnWindows, $"{nameof(ArtifactsChocolateyTest)} works only on Windows agents.");

            return shouldRun;
        }

        public override void Run(BuildContext context)
        {
            if (context.Version == null)
                return;

            context.NuGetInstall("GitVersion.Portable", new NuGetInstallSettings
            {
                Source = new[]
                {
                    context.MakeAbsolute(Paths.Nuget).FullPath
                },
                ExcludeVersion = true,
                Prerelease = true,
                OutputDirectory = Paths.ArtifactsTestBinPortable
            });

            var settings = new GitVersionSettings
            {
                OutputTypes = new HashSet<GitVersionOutput>
                {
                    GitVersionOutput.Json
                },
                ToolPath = Paths.ArtifactsTestBinPortable.Combine("GitVersion.Portable/tools").CombineWithFilePath("gitversion.exe").FullPath
            };
            var gitVersion = context.GitVersion(settings);

            Assert.Equal(context.Version.GitVersion.FullSemVer, gitVersion.FullSemVer);
        }
    }
}
