using Cake.Core;
using Cake.Frosting;
using Common.Utilities;

namespace Build
{
    public class BuildTaskLifetime : FrostingTaskLifetime<BuildContext>
    {
        public override void Setup(BuildContext context, ITaskSetupContext info)
        {
            var message = $"Task: {info.Task.Name}";
            context.StartGroup(message);
        }
        public override void Teardown(BuildContext context, ITaskTeardownContext info)
        {
            context.EndGroup();
        }
    }
}
