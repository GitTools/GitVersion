using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName(nameof(Clean))]
    [TaskDescription("Cleans build artifacts")]
    public sealed class Clean : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.Information("Cleaning directories...");

            context.CleanDirectories(Paths.Src + "/**/bin/" + context.MsBuildConfiguration);
            context.CleanDirectories(Paths.Src + "/**/obj");
            context.CleanDirectories(Paths.TestOutput);
            context.CleanDirectories(Paths.Nuget);
            context.CleanDirectories(Paths.Native);
            context.CleanDirectories(Paths.Packages);
            context.CleanDirectories(Paths.Artifacts);
        }
    }
}
