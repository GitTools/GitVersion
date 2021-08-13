using Cake.Frosting;
using Common.Utilities;

namespace Artifacts.Tasks
{
    [TaskName(nameof(ArtifactsDotnetToolTest))]
    [TaskDescription("Tests the dotnet global tool in docker container")]
    [TaskArgument(Arguments.DockerRegistry, "github", "dockerhub")]
    [TaskArgument(Arguments.DockerDotnetVersion, Constants.Version50, Constants.Version31)]
    [TaskArgument(Arguments.DockerDistro, Constants.Alpine312, Constants.Debian10, Constants.Ubuntu2004)]
    [IsDependentOn(typeof(ArtifactsPrepare))]
    public class ArtifactsDotnetToolTest : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            var shouldRun = true;
            shouldRun &= context.ShouldRun(context.IsDockerOnLinux, $"{nameof(ArtifactsDotnetToolTest)} works only on Docker on Linux agents.");

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
                var cmd = $"-file {rootPrefix}/scripts/Test-DotnetGlobalTool.ps1 -version {version} -repoPath {rootPrefix}/repo -nugetPath {rootPrefix}/nuget";

                context.DockerTestArtifact(dockerImage, cmd, context.DockerRegistry);
            }
        }
    }
}
