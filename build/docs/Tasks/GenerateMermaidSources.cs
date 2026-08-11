using Docs.Utilities;

namespace Docs.Tasks;

[TaskName(nameof(GenerateMermaidSources))]
[TaskDescription("Generates Mermaid documentation sources from integration tests")]
public sealed class GenerateMermaidSources : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.GenerateMermaidSources(check: false);
}
