using Common.Utilities;

namespace Docs.Tasks;

[TaskName(nameof(Clean))]
[TaskDescription("Cleans the temporary publish location")]
public sealed class Clean : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Cleaning directories...");

        context.EnsureDirectoryExists(Paths.ArtifactsDocs.Combine("_published"));
    }
}
