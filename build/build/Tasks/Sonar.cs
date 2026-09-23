namespace Build.Tasks;

[TaskName(nameof(SonarProjectIds))]
[TaskDescription("Generate stable project IDs for the Sonar analysis build")]
public sealed class SonarProjectIds : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => SonarHelper.Run(context, "project-ids", ".", context.Argument<string>("sonar-targets"));
}

[TaskName(nameof(SonarCollect))]
[TaskDescription("Collect and validate the credential-free Sonar analysis handoff")]
public sealed class SonarCollect : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => SonarHelper.Run(context, "collect", ".", ".sonarqube",
        context.Argument<string>("sonar-coverage"), context.Argument<string>("sonar-bundle"), context.Argument<string>("sonar-identity"));
}

internal static class SonarHelper
{
    internal static void Run(BuildContext context, params string[] arguments)
    {
        var args = new ProcessArgumentBuilder().AppendQuoted("build/sonar/Sonar/bin/Release/net10.0/Sonar.dll");
        foreach (var argument in arguments)
        {
            args.AppendQuoted(argument);
        }

        var result = context.StartProcess("dotnet", new ProcessSettings { Arguments = args });
        if (result != 0)
        {
            throw new InvalidOperationException($"Sonar helper failed with exit code {result}");
        }
    }
}
