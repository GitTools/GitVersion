using Common.Utilities;

namespace Chores;

public class BuildLifetime : FrostingLifetime<BuildContext>
{
    public override void Setup(BuildContext context)
    {
        context.StartGroup("Build Setup");
        context.EndGroup();
    }
    public override void Teardown(BuildContext context, ITeardownContext info)
    {
        context.StartGroup("Build Teardown");
        context.EndGroup();
    }
}
