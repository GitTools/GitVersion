using Common.Utilities;

namespace Docker.Tasks;

[TaskName(nameof(DockerBuild))]
[TaskDescription("Build the docker images containing the GitVersion Tool")]
[TaskArgument(Arguments.DockerRegistry, Constants.DockerHub, Constants.GitHub)]
[TaskArgument(Arguments.DockerDotnetVersion, Constants.Version60, Constants.Version31)]
[TaskArgument(Arguments.DockerDistro, Constants.Alpine312, Constants.Debian10, Constants.Ubuntu2004)]
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
        var dest = Paths.Src.Combine("Docker").Combine("content");
        context.EnsureDirectoryExists(dest);
        context.CopyFiles(tool.FullPath, dest);

        foreach (var dockerImage in context.Images)
        {
            if (context.SkipArm64Image(dockerImage)) continue;
            context.DockerBuildImage(dockerImage);
        }
    }
}
