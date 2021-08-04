using Cake.Frosting;

namespace Docker.Tasks
{
    [TaskName(nameof(Docker))]
    [TaskDescription("Run the docker build step")]
    [IsDependentOn(typeof(DockerPublish))]
    public class Docker : FrostingTask<BuildContext>
    {
    }
}
