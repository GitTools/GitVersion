using Common.Utilities;

namespace Common.Lifetime;

public class BuildTaskLifetime : FrostingTaskLifetime
{
    public override void Setup(ICakeContext context, ITaskSetupContext info)
    {
        var message = $"Task: {info.Task.Name}";
        context.StartGroup(message);
    }
    public override void Teardown(ICakeContext context, ITaskTeardownContext info) => context.EndGroup();
}
