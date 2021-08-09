using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Frosting;
using Cake.Wyam;
using Common.Utilities;

namespace Docs.Tasks
{
    [TaskName(nameof(PreviewDocs))]
    [TaskDescription("Run a local server with docs in preview")]
    [IsDependentOn(typeof(Clean))]
    public sealed class PreviewDocs : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            var shouldRun = true;
            shouldRun &= context.ShouldRun(context.DirectoryExists(Paths.Docs), "Wyam documentation directory is missing");

            return shouldRun;
        }

        public override void Run(BuildContext context)
        {
            var additionalSettings = context.WyamAdditionalSettings;
            additionalSettings.Add("Host",  "gittools.github.io");
            context.Wyam(new WyamSettings
            {
                Recipe = "Docs",
                Theme = "Samson",
                OutputPath = context.MakeAbsolute(Paths.ArtifactsDocs.Combine("preview")),
                RootPath = context.MakeAbsolute(Paths.Docs),
                Preview = true,
                Watch = true,
                ConfigurationFile = context.MakeAbsolute(Paths.Docs.CombineWithFilePath("config.wyam")),
                Settings = additionalSettings
            });
        }
    }
}
