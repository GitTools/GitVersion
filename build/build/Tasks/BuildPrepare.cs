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
        context.DotNetRestore(sln,
            new()
            {
                Verbosity = DotNetVerbosity.Minimal,
                Sources = new[] { Constants.NugetOrgUrl },
            });

        context.DotNetBuild("./src/GitVersion.App/GitVersion.App.csproj",
            new()
            {
                Verbosity = DotNetVerbosity.Minimal,
                Configuration = Constants.DefaultConfiguration,
                OutputDirectory = Paths.Tools.Combine("gitversion"),
                Framework = Constants.NetVersionLatest,
                NoRestore = true,
            });

        context.DotNetBuild("./src/GitVersion.Schema/GitVersion.Schema.csproj",
            new()
            {
                Verbosity = DotNetVerbosity.Minimal,
                Configuration = Constants.DefaultConfiguration,
                OutputDirectory = Paths.Tools.Combine("schema"),
                Framework = Constants.NetVersionLatest,
                NoRestore = true,
            });
    }
}
