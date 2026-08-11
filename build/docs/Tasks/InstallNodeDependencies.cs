using Docs.Utilities;

namespace Docs.Tasks;

[TaskName(nameof(InstallNodeDependencies))]
[TaskDescription("Installs the pinned Node.js dependencies used by the documentation build")]
public sealed class InstallNodeDependencies : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.InstallNodeDependencies();
}
