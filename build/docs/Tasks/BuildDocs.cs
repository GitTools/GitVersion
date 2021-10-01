using Cake.Wyam;
using Common.Utilities;

namespace Docs.Tasks;

[TaskName(nameof(BuildDocs))]
[TaskDescription("Builds the docs to local path")]
[IsDependentOn(typeof(Clean))]
public sealed class BuildDocs : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.DirectoryExists(Paths.Docs), "Wyam documentation directory is missing");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        if (context.WyamSettings is not null)
        {
            context.Wyam(context.WyamSettings);
        }
    }
}
