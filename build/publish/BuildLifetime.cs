using Common.Lifetime;
using Common.Utilities;
using Publish.Utilities;

namespace Publish;

public class BuildLifetime : BuildLifetimeBase<BuildContext>
{
    public override void Setup(BuildContext context, ISetupContext info)
    {
        base.Setup(context, info);

        context.Credentials = Credentials.GetCredentials(context);

        if (context.Version?.NugetVersion != null)
        {
            var nugetVersion = context.Version.NugetVersion;

            var nugetPackagesFiles = context.GetFiles(Paths.Nuget + "/*.nupkg");
            foreach (var packageFile in nugetPackagesFiles)
            {
                var packageName = packageFile.GetFilenameWithoutExtension().ToString()[..^(nugetVersion.Length + 1)].ToLower();
                var isChocoPackage = packageName.Equals("GitVersion.Portable", StringComparison.OrdinalIgnoreCase) ||
                                     packageName.Equals("GitVersion", StringComparison.OrdinalIgnoreCase);
                context.Packages.Add(new NugetPackage(packageName, packageFile, isChocoPackage));
            }
        }
        context.StartGroup("Build Setup");
        LogBuildInformation(context);
        context.EndGroup();
    }
}
