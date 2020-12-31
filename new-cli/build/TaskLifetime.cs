using Cake.Core;
using Cake.Frosting;

public class TaskLifetime : FrostingTaskLifetime<Context>
{
    public override void Setup(Context context, ITaskSetupContext info)
    {
        var message = $"Task: {info.Task.Name}";
        context.StartGroup(message);
    }

    public override void Teardown(Context context, ITaskTeardownContext info)
    {
        context.EndGroup();
    }
}