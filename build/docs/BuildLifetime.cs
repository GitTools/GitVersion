using System.Collections.Generic;
using Cake.Common.IO;
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
            context.WyamAdditionalSettings = new Dictionary<string, object>
            {
                { "BaseEditUrl", "https://github.com/gittools/GitVersion/tree/main/docs/input/" },
                { "SourceFiles", context.MakeAbsolute(Paths.Src) + "/**/{!bin,!obj,!packages,!*.Tests,!GitTools.*,}/**/*.cs" },
                { "Title", "GitVersion" },
                { "IncludeGlobalNamespace", false }
            };

            context.StartGroup("Build Setup");

            LogBuildInformation(context);

            context.EndGroup();
        }
    }
}
