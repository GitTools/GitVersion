using System.Linq;
using Cake.Common.IO;
using Cake.Common.Tools.Chocolatey;
using Cake.Common.Tools.Chocolatey.Pack;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName(nameof(PackageChocolatey))]
    [TaskDescription("Creates the chocolatey packages")]
    public class PackageChocolatey : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            PackPrepareNative(context);
            context.EnsureDirectoryExists(Paths.Nuget);

            foreach (var package in context.Packages!.Chocolatey)
            {
                if (context.FileExists(package.NuspecPath))
                {
                    var artifactPath = context.MakeAbsolute(context.PackagesBuildMap[package.Id]).FullPath;

                    var chocolateySettings = new ChocolateyPackSettings
                    {
                        LimitOutput = true,
                        Version = context.Version?.SemVersion,
                        OutputDirectory = Paths.Nuget,
                        Files = context.GetFiles(artifactPath + "/**/*.*")
                            .Select(file => new ChocolateyNuSpecContent { Source = file.FullPath, Target = file.FullPath.Replace(artifactPath, "") })
                            .ToArray()
                    };
                    context.ChocolateyPack(package.NuspecPath, chocolateySettings);
                }
            }
        }

        private static void PackPrepareNative(ICakeContext context)
        {
            var sourceDir = DirectoryPath.FromString(Paths.Native).Combine(PlatformFamily.Windows.ToString()).Combine("win-x64");
            var sourceFiles = context.GetFiles(sourceDir + "/*.*");

            var portableDir = DirectoryPath.FromString(Paths.ArtifactsBinPortable).Combine("tools");
            context.EnsureDirectoryExists(portableDir);

            sourceFiles += context.GetFiles("./build/nuspec/*.ps1") + context.GetFiles("./build/nuspec/*.txt");
            context.CopyFiles(sourceFiles, portableDir);
        }
    }
}
