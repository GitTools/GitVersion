using Docs.Utilities;

namespace Docs.Tasks;

[TaskName(nameof(ValidateMermaidDiagrams))]
[TaskDescription("Verifies generated Mermaid sources and validates their syntax")]
[IsDependentOn(typeof(InstallNodeDependencies))]
public sealed class ValidateMermaidDiagrams : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.GenerateMermaidSources(check: true);
        context.ValidateMermaidSyntax();
    }
}
