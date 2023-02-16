using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(BuildPrepare))]
[TaskDescription("Builds the solution")]
[IsDependentOn(typeof(Clean))]
public sealed class BuildPrepare : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Builds solution...");

        const string sln = "./src/GitVersion.sln";
        const string project = "./src/GitVersion.App/GitVersion.App.csproj";
        context.DotNetRestore(sln,
            new()
            {
                Verbosity = DotNetVerbosity.Minimal,
                Sources = new[] { Constants.NugetOrgUrl },
            });

        context.DotNetBuild(project,
            new()
            {
                Verbosity = DotNetVerbosity.Minimal,
                Configuration = Constants.DefaultConfiguration,
                OutputDirectory = Paths.Dogfood,
                Framework = Constants.NetVersionLatest,
                NoRestore = true,
            });
    }
}
