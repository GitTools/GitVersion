using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(PackageLinux))]
[TaskDescription("Builds native Linux packages in a glibc or musl Docker image")]
[TaskArgument(Arguments.NativeContainer, "glibc", "musl")]
public sealed class PackageLinux : FrostingTask<BuildContext>
{
    private static readonly string[] BuildEnvironment =
    [
        "CI", "GITHUB_ACTIONS", "GITHUB_REF", "GITHUB_REF_TYPE", "GITHUB_SHA",
        "GITHUB_REPOSITORY", "GITHUB_RUN_ID", "GITHUB_RUN_NUMBER", "GITHUB_EVENT_NAME",
        "GITHUB_HEAD_REF", "GITHUB_BASE_REF", "GITHUB_WORKFLOW", "GITHUB_JOB"
    ];

    public override void Run(BuildContext context)
    {
        if (context.IsOnWindows)
        {
            throw new CakeException("Linux container packaging requires a Linux or macOS host with Docker.");
        }

        var container = context.Argument(Arguments.NativeContainer, "glibc");
        if (container is not ("glibc" or "musl"))
        {
            throw new CakeException("The native_container argument must be glibc or musl.");
        }

        var root = context.MakeAbsolute(Paths.Root);
        var statArguments = context.IsOnMacOS ? "-f %u:%g ." : "-c %u:%g .";
        var owner = context.ExecuteCommand("stat", statArguments, root).Single().Trim().Split(':');
        if (owner.Length != 2 || !uint.TryParse(owner[0], out var uid) || !uint.TryParse(owner[1], out var gid) || uid == 0)
        {
            throw new CakeException("Native container packaging requires a non-root checkout owner.");
        }

        // Worktree .git files contain absolute paths outside the checkout. Preserve
        // those paths and mount the common Git directory so local worktrees work too.
        var gitDirectory = context.ExecuteCommand("git", "rev-parse --path-format=absolute --git-common-dir", root).Single().Trim();
        var image = $"gitversion-native-package-{container}";
        context.DockerBuild(new DockerImageBuildSettings { Target = container, Tag = [image] },
            context.MakeAbsolute(Paths.Build.Combine("nativeaot")).FullPath);

        var args = new ProcessArgumentBuilder()
            .Append("run").Append("--rm")
            .Append("--user").AppendQuoted($"{uid}:{gid}")
            .Append("--volume").AppendQuoted($"{root.FullPath}:{root.FullPath}")
            .Append("--volume").AppendQuoted($"{gitDirectory}:{gitDirectory}")
            .Append("--workdir").AppendQuoted(root.FullPath)
            .Append("--env").AppendQuoted("GIT_CONFIG_COUNT=1")
            .Append("--env").AppendQuoted("GIT_CONFIG_KEY_0=safe.directory")
            .Append("--env").AppendQuoted($"GIT_CONFIG_VALUE_0={root.FullPath}");
        foreach (var name in BuildEnvironment)
        {
            var value = context.EnvironmentVariable(name);
            if (value is not null)
            {
                args.Append("--env").AppendQuoted($"{name}={value}");
            }
        }
        args.Append(image).Append("dotnet").Append("run/build.dll").Append("--target=Package")
            .AppendQuoted($"--configuration={context.MsBuildConfiguration}");

        var exitCode = context.StartProcess("docker", new ProcessSettings { Arguments = args, WorkingDirectory = root });
        if (exitCode != 0)
        {
            throw new CakeException($"Native {container} packaging failed (exit {exitCode}).");
        }
    }
}
