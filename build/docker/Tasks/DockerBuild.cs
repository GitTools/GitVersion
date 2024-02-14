using Common.Utilities;

namespace Docker.Tasks;

[TaskName(nameof(DockerBuild))]
[TaskDescription("Build the docker images containing the GitVersion Tool")]
[TaskArgument(Arguments.DockerRegistry, Constants.DockerHub, Constants.GitHub)]
[TaskArgument(Arguments.DockerDotnetVersion, Constants.Version60, Constants.Version70, Constants.Version80)]
[TaskArgument(Arguments.DockerDistro, Constants.AlpineLatest, Constants.DebianLatest, Constants.UbuntuLatest)]
[TaskArgument(Arguments.Architecture, Constants.Amd64, Constants.Arm64)]
public class DockerBuild : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsDockerOnLinux, $"{nameof(DockerBuild)} works only on Docker on Linux agents.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        var tool = Paths.Nuget.CombineWithFilePath("GitVersion.Tool*");
        var dest = Paths.Build.Combine("docker").Combine("nuget");
        context.EnsureDirectoryExists(dest);
        context.CopyFiles(tool.FullPath, dest);

        foreach (var dockerImage in context.Images)
        {
            context.DockerBuildImage(dockerImage);
        }
    }
}
