using Docs.Utilities;

namespace Docs.Tasks;

[TaskName(nameof(PrepareDocsInputs))]
[TaskDescription("Resolve documentation trains and cache their release inputs")]
public sealed class PrepareDocsInputs : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.DocumentationInputs = new DocsInputs(
        context.Environment.WorkingDirectory.FullPath, message => context.Information(message)).Prepare().GetAwaiter().GetResult();
}
