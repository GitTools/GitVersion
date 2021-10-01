using Cake.Wyam;
using Common.Utilities;

namespace Docs.Tasks;

[TaskName(nameof(PreviewDocs))]
[TaskDescription("Run a local server with docs in preview")]
[IsDependentOn(typeof(Clean))]
public sealed class PreviewDocs : FrostingTask<BuildContext>
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
            context.WyamSettings.Preview = true;
            context.WyamSettings.Watch = true;
            context.WyamSettings.Settings.Add("Host", "gittools.github.io");
            context.Wyam(context.WyamSettings);
        }
    }
}
