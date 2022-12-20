using Cake.Wyam;
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

        context.WyamSettings = new WyamSettings
        {
            Recipe = "Docs",
            Theme = "Samson",
            OutputPath = context.MakeAbsolute(Paths.ArtifactsDocs.Combine("preview")),
            RootPath = context.MakeAbsolute(Paths.Docs),
            ConfigurationFile = context.MakeAbsolute(Paths.Docs.CombineWithFilePath("config.wyam")),
            Settings = new Dictionary<string, object>
            {
                { "BaseEditUrl", "https://github.com/gittools/GitVersion/tree/main/docs/input/" },
                { "SourceFiles", context.MakeAbsolute(Paths.Src) + "/**/{!bin,!obj,!packages,!*.Tests,!GitTools.*,}/**/*.cs" },
                { "Title", "GitVersion" },
                { "IncludeGlobalNamespace", false }
            }
        };

        context.StartGroup("Build Setup");

        LogBuildInformation(context);

        context.EndGroup();
    }
}
