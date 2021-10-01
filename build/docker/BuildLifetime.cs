using Common.Utilities;

namespace Docker;

public class BuildLifetime : BuildLifetimeBase<BuildContext>
{
    public override void Setup(BuildContext context)
    {
        base.Setup(context);

        context.IsDockerOnLinux = context.DockerCustomCommand("info --format '{{.OSType}}'").First().Replace("'", "") == "linux";

        var architecture = context.HasArgument(Arguments.Architecture) ? context.Argument<Architecture>(Arguments.Architecture) : (Architecture?)null;
        var dockerRegistry = context.Argument(Arguments.DockerRegistry, DockerRegistry.DockerHub);
        var dotnetVersion = context.Argument(Arguments.DockerDotnetVersion, string.Empty).ToLower();
        var dockerDistro = context.Argument(Arguments.DockerDistro, string.Empty).ToLower();

        var versions = string.IsNullOrWhiteSpace(dotnetVersion) ? Constants.VersionsToBuild : new[] { dotnetVersion };
        var distros = string.IsNullOrWhiteSpace(dockerDistro) ? Constants.DockerDistrosToBuild : new[] { dockerDistro };
        var archs = architecture.HasValue ? new[] { architecture.Value } : Constants.ArchToBuild;

        var registry = dockerRegistry == DockerRegistry.DockerHub ? Constants.DockerHubRegistry : Constants.GitHubContainerRegistry;
        context.DockerRegistry = dockerRegistry;
        context.Images = from version in versions
                         from distro in distros
                         from arch in archs
                         select new DockerImage(distro, version, arch, registry, false);

        context.StartGroup("Build Setup");

        LogBuildInformation(context);

        context.Information("IsDockerOnLinux:   {0}", context.IsDockerOnLinux);
        context.Information($"Building for Version: {dotnetVersion}, Distro: {dockerDistro}");
        context.EndGroup();
    }
}
