using Build.Utilities;
using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(ValidateVersion))]
[TaskDescription("Validates built assembly version")]
[IsDependentOn(typeof(Build))]
public class ValidateVersion : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var gitversionTool = context.GetGitVersionToolLocation();
        context.ValidateOutput("dotnet", $"\"{gitversionTool}\" -version", context.Version!.GitVersion!.InformationalVersion!);
    }
}
