using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName(nameof(Test))]
    [TaskDescription("Run the tests. Can be specified the target framework with --dotnet_target=")]
    [IsDependentOn(typeof(PublishCoverage))]
    public class Test : FrostingTask<BuildContext>
    {
    }
}
