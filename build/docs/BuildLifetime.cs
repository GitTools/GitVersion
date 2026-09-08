using Common.Lifetime;
using Common.Utilities;
using Docs.Utilities;

namespace Docs;

public class BuildLifetime : BuildLifetimeBase<BuildContext>
{
    public override void Setup(BuildContext context, ISetupContext info)
    {
        base.Setup(context, info);

        context.Credentials = Credentials.GetCredentials(context);
        context.ForcePublish = context.HasArgument("force");

        context.StartGroup("Build Setup");

        LogBuildInformation(context);

        context.EndGroup();
    }
}
