using Cake.Core;
using Cake.Frosting;

public class TaskLifetime : FrostingTaskLifetime<Context>
{
    public override void Setup(Context context, ITaskSetupContext info)
    {
    }

    public override void Teardown(Context context, ITaskTeardownContext info)
    {
    }
}