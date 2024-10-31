using Common.Utilities;

namespace Config;

public class BuildLifetime : FrostingLifetime<BuildContext>
{
    public override void Setup(BuildContext context, ISetupContext info)
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
