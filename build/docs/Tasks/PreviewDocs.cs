using Common.Utilities;
using Docs.Utilities;

namespace Docs.Tasks;

[TaskName(nameof(PreviewDocs))]
[TaskDescription("Run a local server with docs in preview")]
[IsDependentOn(typeof(BuildDocs))]
public sealed class PreviewDocs : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.DirectoryExists(Paths.Docs), "Wyam documentation directory is missing");

        return shouldRun;
    }

    public override void Run(BuildContext context) => VersionedDocs.Preview(context);
}
