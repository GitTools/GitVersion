using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName(nameof(Test))]
    [TaskDescription("(CI only) Run the tests and publish the results")]
    [IsDependentOn(typeof(UnitTest))]
    public class Test : FrostingTask<BuildContext>
    {
    }
}
