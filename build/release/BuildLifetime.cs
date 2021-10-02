using Common.Utilities;
using Release.Utilities;

namespace Release;

public class BuildLifetime : BuildLifetimeBase<BuildContext>
{
    public override void Setup(BuildContext context)
    {
        base.Setup(context);
        context.Credentials = Credentials.GetCredentials(context);

        context.StartGroup("Build Setup");
        LogBuildInformation(context);
        context.EndGroup();
    }
}
