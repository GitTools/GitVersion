using Cake.Common.Tools.DotNet.Restore;

namespace Build.Tasks;

[TaskName(nameof(Build))]
[TaskDescription("Builds the solution")]
[IsDependentOn(typeof(Clean))]
// [IsDependentOn(typeof(CodeFormat))]
public sealed class Build : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Builds solution...");
        const string sln = "./src/GitVersion.sln";

        context.DotNetRestore(sln, new DotNetRestoreSettings
        {
            Verbosity = DotNetVerbosity.Minimal,
            Sources = new[] { Constants.NugetOrgUrl },
            MSBuildSettings = context.MsBuildSettings
        });

        context.DotNetBuild(sln, new DotNetBuildSettings
        {
            Verbosity = DotNetVerbosity.Minimal,
            Configuration = context.MsBuildConfiguration,
            NoRestore = true,
            MSBuildSettings = context.MsBuildSettings
        });
    }
}
