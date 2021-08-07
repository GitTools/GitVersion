using System.Collections.Generic;
using Cake.Common.IO;
using Common.Utilities;
using Publish.Utilities;

namespace Publish
{
    public class BuildLifetime : BuildLifetimeBase<BuildContext>
    {
        public override void Setup(BuildContext context)
        {
            base.Setup(context);
            context.Credentials = Credentials.GetCredentials(context);

            if (context.Version?.NugetVersion != null)
            {
                var nugetVersion = context.Version.NugetVersion;
                context.Packages = new List<NugetPackage>();

                var nugetPackagesFiles = context.GetFiles(Paths.Nuget + "/*.nupkg");
                foreach (var packageFile in nugetPackagesFiles)
                {
                    var packageName = packageFile.GetFilenameWithoutExtension().ToString()[..^(nugetVersion.Length + 1)].ToLower();
                    context.Packages.Add(new NugetPackage(packageName, packageFile, packageName.Contains("Portable")));
                }
            }
            context.StartGroup("Build Setup");
            LogBuildInformation(context);
            context.EndGroup();
        }
    }
}
