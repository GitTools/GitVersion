using Cake.Json;

namespace Config.Tasks;

public class SetMatrix : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        const string coreProject = "GitVersion.Core.Tests";
        var projects = context.GetFiles("./src/**/*.Tests.csproj")
            .Select(project => project.GetFilenameWithoutExtension().FullPath).Order(StringComparer.Ordinal).ToArray();
        if (!projects.Contains(coreProject, StringComparer.Ordinal))
        {
            throw new CakeException($"Test project {coreProject} was not found.");
        }
        var otherProjects = string.Join(',', projects.Where(project => project != coreProject));
        var rows = new List<object>();
        foreach (var os in new[] { "windows-2025-vs2026", "ubuntu-24.04", "macos-26" })
        {
            foreach (var framework in Constants.DotnetVersions)
            {
                var name = $"{os} - net{framework}";
                // Git CLI fixture setup is expensive on macOS/net10. Keep every test
                // and the existing per-job budget by partitioning the covered target.
                if (os == "macos-26" && framework == Constants.DotnetLtsLatest)
                {
                    var groups = new[]
                    {
                        new { name = "Core integration", id = "core-integration", filter = "FullyQualifiedName~GitVersion.Tests.IntegrationTests&FullyQualifiedName!~Mainline" },
                        new { name = "Core integration GitFlow", id = "core-integration-gitflow", filter = "FullyQualifiedName~GitVersion.Tests.IntegrationTests&FullyQualifiedName~Mainline&FullyQualifiedName~GitFlow" },
                        new { name = "Core integration other", id = "core-integration-other", filter = "FullyQualifiedName~GitVersion.Tests.IntegrationTests&FullyQualifiedName~Mainline&FullyQualifiedName!~GitFlow" },
                        new { name = "Core remaining", id = "core-other", filter = "FullyQualifiedName!~GitVersion.Tests.IntegrationTests" }
                    };
                    foreach (var group in groups)
                    {
                        rows.Add(new { name = $"{name} - {group.name}", os, dotnet_version = framework, group = group.id, test_projects = coreProject, test_filter = group.filter });
                    }
                    rows.Add(new { name = $"{name} - CLI and adapters", os, dotnet_version = framework, group = "other-projects", test_projects = otherProjects, test_filter = "" });
                }
                else
                {
                    rows.Add(new { name, os, dotnet_version = framework, group = "all", test_projects = "", test_filter = "" });
                }
            }
        }
        var testMatrix = new { include = rows };
        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            context.GitHubActions().Commands.SetOutputParameter("docker_distros", context.SerializeJson(Constants.DockerDistros));
            context.GitHubActions().Commands.SetOutputParameter("dotnet_versions", context.SerializeJson(Constants.DotnetVersions));
            context.GitHubActions().Commands.SetOutputParameter("test_matrix", context.SerializeJson(testMatrix));
        }
        else
        {
            context.Information("Docker Distros: {0}", context.SerializeJson(Constants.DockerDistros));
            context.Information("Dotnet Versions: {0}", context.SerializeJson(Constants.DotnetVersions));
            context.Information("Test Matrix: {0}", context.SerializeJson(testMatrix));
        }
    }
}
