using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName(nameof(PackageNuget))]
    [TaskDescription("Creates the nuget packages")]
    public class PackageNuget : FrostingTask<BuildContext>
    {
    }
}
