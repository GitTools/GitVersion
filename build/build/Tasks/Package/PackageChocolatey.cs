using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName(nameof(PackageChocolatey))]
    [TaskDescription("Creates the chocolatey packages")]
    public class PackageChocolatey : FrostingTask<BuildContext>
    {
    }
}
