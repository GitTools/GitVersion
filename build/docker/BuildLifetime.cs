using System.Linq;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Docker;
using Common.Utilities;
using Constants = Common.Utilities.Constants;

namespace Docker
{
    public class BuildLifetime : BuildLifetimeBase<BuildContext>
    {
        public override void Setup(BuildContext context)
        {
            base.Setup(context);

            context.IsDockerOnLinux = context.DockerCustomCommand("info --format '{{.OSType}}'").First().Replace("'", "") == "linux";

            var dotnetVersion = context.Argument("docker_dotnetversion", "").ToLower();
            var dockerDistro = context.Argument("docker_distro", "").ToLower();

            var versions = string.IsNullOrWhiteSpace(dotnetVersion) ? Constants.VersionsToBuild : new[] { dotnetVersion };
            var distros = string.IsNullOrWhiteSpace(dockerDistro) ? Constants.DockerDistrosToBuild : new[] { dockerDistro };

            context.Images = from version in versions
                             from distro in distros
                             select new DockerImage(distro, version);

            context.StartGroup("Build Setup");

            LogBuildInformation(context);

            context.Information("IsDockerOnLinux:   {0}", context.IsDockerOnLinux);
            context.Information($"Building for Version: {dotnetVersion}, Distro: {dockerDistro}");
            context.EndGroup();
        }
    }
}
