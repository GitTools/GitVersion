using Cake.Json;

namespace Config.Tasks;

public class SetMatrix : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var packageBuildMatrix = new
        {
            include = new[]
            {
                new { name = "windows-2025-vs2026", os = "windows-2025-vs2026", rid = "windows", artifact = "Windows", container = "" },
                new { name = "macos-26", os = "macos-26", rid = "osx-arm64", artifact = "macOS", container = "" },
                new { name = "ubuntu-24.04", os = "ubuntu-24.04", rid = "linux-x64", artifact = "linux-x64", container = "glibc" },
                new { name = "linux-arm64", os = "ubuntu-24.04-arm", rid = "linux-arm64", artifact = "linux-arm64", container = "glibc" },
                new { name = "linux-musl-x64", os = "ubuntu-24.04", rid = "linux-musl-x64", artifact = "linux-musl-x64", container = "musl" },
                new { name = "linux-musl-arm64", os = "ubuntu-24.04-arm", rid = "linux-musl-arm64", artifact = "linux-musl-arm64", container = "musl" }
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
