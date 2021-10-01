using Cake.Common.Tools.DotNetCore.Restore;

namespace Build.Tasks;

[TaskName(nameof(Build))]
[TaskDescription("Builds the solution")]
[IsDependentOn(typeof(Clean))]
[IsDependentOn(typeof(CodeFormat))]
public sealed class Build : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Builds solution...");
        const string sln = "./src/GitVersion.sln";

        context.DotNetCoreRestore(sln, new DotNetCoreRestoreSettings
        {
            Verbosity = DotNetCoreVerbosity.Minimal,
            Sources = new[] { "https://api.nuget.org/v3/index.json" },
            MSBuildSettings = context.MsBuildSettings
        });

        context.DotNetCoreBuild(sln, new DotNetCoreBuildSettings
        {
            Verbosity = DotNetCoreVerbosity.Minimal,
            Configuration = context.MsBuildConfiguration,
            NoRestore = true,
            MSBuildSettings = context.MsBuildSettings
        });
    }
}
