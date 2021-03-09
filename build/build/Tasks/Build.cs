using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.Restore;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName(nameof(Build))]
    [TaskDescription("Builds the solution")]
    public sealed class Build : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.Information("Builds solution...");
            var sln = "./src/GitVersion.sln";

            context.DotNetCoreRestore(sln, new DotNetCoreRestoreSettings
            {
                Verbosity = DotNetCoreVerbosity.Minimal,
                Sources = new [] { "https://api.nuget.org/v3/index.json" },
                MSBuildSettings = context.MSBuildSettings
            });

            var slnPath = context.MakeAbsolute(new DirectoryPath(sln));
            context.DotNetCoreBuild(sln, new DotNetCoreBuildSettings
            {
                Verbosity = DotNetCoreVerbosity.Minimal,
                Configuration = context.Configuration,
                NoRestore = true,
                MSBuildSettings = context.MSBuildSettings
            });
        }
    }
}
