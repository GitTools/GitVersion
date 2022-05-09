using Common.Utilities;

namespace Artifacts.Tasks;

[TaskName(nameof(ArtifactsMsBuildCoreTest))]
[TaskDescription("Tests the msbuild package in docker container")]
[TaskArgument(Arguments.DockerRegistry, Constants.DockerHub, Constants.GitHub)]
[TaskArgument(Arguments.DockerDotnetVersion, Constants.Version60, Constants.Version31)]
[TaskArgument(Arguments.DockerDistro, Constants.Alpine312, Constants.Debian10, Constants.Ubuntu2004)]
[IsDependentOn(typeof(ArtifactsPrepare))]
public class ArtifactsMsBuildCoreTest : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsDockerOnLinux, $"{nameof(ArtifactsMsBuildCoreTest)} works only on Docker on Linux agents.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        if (context.Version == null)
            return;
        var rootPrefix = string.Empty;
        var version = context.Version.NugetVersion;

        foreach (var dockerImage in context.Images)
        {
            if (context.SkipArm64Image(dockerImage)) continue;

            string distro = dockerImage.Distro;
            string targetFramework = dockerImage.TargetFramework;

            if (targetFramework == Constants.Version31 && distro == Constants.Centos8) continue; // TODO check why this one fails
            targetFramework = targetFramework switch
            {
                Constants.Version31 => $"netcoreapp{targetFramework}",
                Constants.Version60 => $"net{targetFramework}",
                _ => targetFramework
            };

            var cmd = $"{rootPrefix}/scripts/test-msbuild-task.sh --version {version} --nugetPath {rootPrefix}/nuget --repoPath {rootPrefix}/repo/tests/integration/core --targetframework {targetFramework}";

            context.DockerTestArtifact(dockerImage, cmd);
        }
    }
}
