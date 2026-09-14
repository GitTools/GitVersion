using Cake.Common.Tools.DotNet.Pack;
using Cake.Common.Tools.DotNet.Publish;
using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(PackageNuget))]
[TaskDescription("Creates the nuget packages")]
public class PackageNuget : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.EnsureDirectoryExists(Paths.Nuget);

        PackageWithCli(context);
    }
    private static void PackageWithCli(BuildContext context)
    {
        var settings = new DotNetPackSettings
        {
            Configuration = context.MsBuildConfiguration,
            OutputDirectory = Paths.Nuget,
            MSBuildSettings = context.MsBuildSettings,
        };

        // GitVersion.MsBuild, global tool & core
        context.DotNetPack("./src/GitVersion.Core", settings);

        settings.ArgumentCustomization = arg => arg.Append("-p:PackAsTool=true").Append("-p:BuildInParallel=false");
        context.DotNetPack("./src/GitVersion.App", settings);

        foreach (var version in Constants.DotnetVersions)
        {
            context.DotNetPublish("./src/GitVersion.App", new DotNetPublishSettings
            {
                Configuration = context.MsBuildConfiguration,
                Framework = $"net{version}",
                MSBuildSettings = context.MsBuildSettings
            });
        }

        settings.ArgumentCustomization = arg => arg.Append("-p:IsPackaging=true");
        context.DotNetPack("./src/GitVersion.MsBuild", settings);
    }
}
