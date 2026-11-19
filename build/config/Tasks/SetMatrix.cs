using Cake.Json;

namespace Config.Tasks;

public class SetMatrix : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        const string linuxRunner = "ubuntu-26.04";
        const string macosRunner = "macos-26";
        var packageBuildMatrix = new
        {
            include = new[]
            {
                new { name = "windows-2025-vs2026", os = "windows-2025-vs2026", rid = "windows", artifact = "Windows", container = "" },
                new { name = macosRunner, os = macosRunner, rid = "osx-arm64", artifact = "macOS", container = "" },
                new { name = linuxRunner, os = linuxRunner, rid = "linux-x64", artifact = "linux-x64", container = "glibc" },
                new { name = "linux-arm64", os = "ubuntu-26.04-arm", rid = "linux-arm64", artifact = "linux-arm64", container = "glibc" },
                new { name = "linux-musl-x64", os = linuxRunner, rid = "linux-musl-x64", artifact = "linux-musl-x64", container = "musl" },
                new { name = "linux-musl-arm64", os = "ubuntu-26.04-arm", rid = "linux-musl-arm64", artifact = "linux-musl-arm64", container = "musl" }
            }
        };
        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            context.GitHubActions().Commands.SetOutputParameter("docker_distros", context.SerializeJson(Constants.DockerDistros));
            context.GitHubActions().Commands.SetOutputParameter("dotnet_versions", context.SerializeJson(Constants.DotnetVersions));
            context.GitHubActions().Commands.SetOutputParameter("build_matrix", context.SerializeJson(packageBuildMatrix));
        }
        else
        {
            context.Information("Docker Distros: {0}", context.SerializeJson(Constants.DockerDistros));
            context.Information("Dotnet Versions: {0}", context.SerializeJson(Constants.DotnetVersions));
            context.Information("Build Matrix: {0}", context.SerializeJson(packageBuildMatrix));
        }
    }
}
