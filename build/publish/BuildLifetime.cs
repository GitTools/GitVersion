using Common.Utilities;
using Publish.Utilities;

namespace Publish
{
    public class BuildLifetime : BuildLifetimeBase<BuildContext>
    {
        public override void Setup(BuildContext context)
        {
            base.Setup(context);
            context.Credentials = BuildCredentials.GetCredentials(context);

            context.StartGroup("Build Setup");
            LogBuildInformation(context);
            context.EndGroup();
        }
    }
}
