using Cake.Common.Tools.DotNet.Pack;
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

        settings.ArgumentCustomization = arg => arg.Append("-p:PackAsTool=true");
        context.DotNetPack("./src/GitVersion.App", settings);

        settings.ArgumentCustomization = arg => arg.Append("-p:IsPackaging=true");
        context.DotNetPack("./src/GitVersion.MsBuild", settings);
    }
}
