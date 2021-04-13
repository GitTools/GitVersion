using System.Linq;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.Chocolatey;
using Cake.Common.Tools.Chocolatey.Pack;
using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName(nameof(PackageChocolatey))]
    [TaskDescription("Creates the chocolatey packages")]
    [IsDependentOn(typeof(PackagePrepare))]
    public class PackageChocolatey : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            if (context.IsRunningOnWindows())
            {
                context.Information("Pack-Chocolatey works only on Windows agents.");
                return true;
            }
            return false;
        }

        public override void Run(BuildContext context)
        {
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
    }
}
