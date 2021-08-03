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
    [TaskName(nameof(ArtifactsCmdlineTest))]
    [TaskDescription("Tests the cmdline package on windows")]
    public class ArtifactsCmdlineTest : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            var shouldRun = true;
            shouldRun &= context.ShouldRun(context.IsOnWindows, $"{nameof(ArtifactsCmdlineTest)} works only on Windows agents.");

            return shouldRun;
        }

        public override void Run(BuildContext context)
        {
            if (context.Version == null)
                return;

            context.NuGetInstall("GitVersion.Commandline", new NuGetInstallSettings
            {
                Source = new[]
                {
                    context.MakeAbsolute(Paths.Nuget).FullPath
                },
                ExcludeVersion = true,
                Prerelease = true,
                OutputDirectory = Paths.ArtifactsTestBinCmdline
            });

            var settings = new GitVersionSettings
            {
                OutputTypes = new HashSet<GitVersionOutput>
                {
                    GitVersionOutput.Json
                },
                ToolPath = Paths.ArtifactsTestBinCmdline.Combine("GitVersion.Commandline/tools").CombineWithFilePath("gitversion.exe").FullPath
            };
            var gitVersion = context.GitVersion(settings);

            Assert.Equal(context.Version.GitVersion.FullSemVer, gitVersion.FullSemVer);
        }
    }
}
