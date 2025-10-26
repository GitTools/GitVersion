using Common.Lifetime;
using Common.Utilities;
using Docker.Utilities;

namespace Docker;

public class BuildLifetime : BuildLifetimeBase<BuildContext>
{
    public override void Setup(BuildContext context, ISetupContext info)
    {
        base.Setup(context, info);

        context.Credentials = Credentials.GetCredentials(context);

        context.IsDockerOnLinux = context.DockerCustomCommand("info --format '{{.OSType}}'").First().Replace("'", "") == "linux";

        var dockerRegistry = context.Argument(Arguments.DockerRegistry, DockerRegistry.DockerHub);
        var dotnetVersion = context.Argument(Arguments.DotnetVersion, string.Empty).ToLower();
        var dockerDistro = context.Argument(Arguments.DockerDistro, string.Empty).ToLower();

        var versions = string.IsNullOrWhiteSpace(dotnetVersion)
            ? Constants.DotnetVersions
            : string.Equals(dotnetVersion, "lts-latest", StringComparison.OrdinalIgnoreCase)
                ? [Constants.DotnetLtsLatest]
                : [dotnetVersion];

        var distros = string.IsNullOrWhiteSpace(dockerDistro)
            ? Constants.DockerDistros
            : string.Equals(dockerDistro, "distro-latest", StringComparison.OrdinalIgnoreCase)
                ? [Constants.AlpineLatest]
                : [dockerDistro];

        var architectures = context.HasArgument(Arguments.Architecture) ? context.Arguments<Architecture>(Arguments.Architecture) : Constants.ArchToBuild;
        var platformArch = context.IsRunningOnAmd64() ? Architecture.Amd64 : Architecture.Arm64;

        var registry = dockerRegistry == DockerRegistry.DockerHub ? Constants.DockerHubRegistry : Constants.GitHubContainerRegistry;

        context.DockerRegistry = dockerRegistry;
        context.Architectures = architectures;
        context.Images = from version in versions
                         from distro in distros
                         from arch in architectures
                         select new DockerImage(distro, version, arch, registry, false);

        context.StartGroup("Build Setup");

        LogBuildInformation(context);

        context.Information($"IsDockerOnLinux:      {context.IsDockerOnLinux}");
        context.Information($"Building for Version: {dotnetVersion}, Distro: {dockerDistro}, Architecture: {platformArch}");
        context.EndGroup();
    }
}
