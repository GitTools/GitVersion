using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Frosting;
using Common.Utilities;

namespace Docker.Tasks
{
    [TaskName(nameof(DockerBuild))]
    [TaskDescription("Build the docker images containing the GitVersion Tool")]
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
            var dest = DirectoryPath.FromString($"./src/Docker").Combine("content");
            context.EnsureDirectoryExists(dest);
            context.CopyFiles(tool.FullPath, dest);

            foreach (var dockerImage in context.Images)
            {
                context.DockerBuild(dockerImage, context.DockerRegistry);
            }
        }
    }
}
