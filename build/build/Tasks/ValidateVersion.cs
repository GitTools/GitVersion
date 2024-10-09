using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(ValidateVersion))]
[TaskDescription("Validates built assembly version")]
[IsDependentOn(typeof(Build))]
public class ValidateVersion : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context.Version);
        var gitVersionTool = context.GetGitVersionToolLocation();
        context.ValidateOutput("dotnet", $"\"{gitVersionTool}\" -version", context.Version.GitVersion.InformationalVersion);
    }
}
