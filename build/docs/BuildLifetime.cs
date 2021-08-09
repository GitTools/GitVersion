using Common.Utilities;
using Docs.Utilities;

namespace Docs
{
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
}
