using GitVersion.Helpers;
using GitVersion.Testing.Extensions;

namespace GitVersion.App.Tests;

[TestFixture]
[NonParallelizable]
public class BranchContextCommandTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task DynamicRepositoryUsesResolvedClonePath(bool contextual)
    {
        using var remote = new EmptyRepositoryFixture();
        remote.Repository.MakeATaggedCommit("1.0.0");
        var head = remote.Repository.MakeACommit();
        var scratch = Directory.CreateTempSubdirectory("gitversion-3954-cli-");
        try
        {
            var working = Directory.CreateDirectory(Path.Combine(scratch.FullName, "working"));
            var program = new ProgramFixture(working.FullName);
            var args = new List<string>
            {
                "--url", remote.RepositoryPath.TrimEnd(Path.DirectorySeparatorChar),
                "--dynamic-repo-location", Path.Combine(scratch.FullName, "clones"),
                "--no-fetch", "--no-cache"
            };
            if (contextual)
            {
                program.WithEnv(new KeyValuePair<string, string>("GIT_BRANCH", "feature/dynamic"));
            }
            else
            {
                args.AddRange(["--branch", "main"]);
            }

            var result = await program.Run(args: [.. args]);

            result.ExitCode.ShouldBe(0, result.Log);
            var variables = result.OutputVariables.ShouldNotBeNull();
            variables.Sha.ShouldBe(head.Sha);
            variables.BranchName.ShouldBe(contextual ? "feature/dynamic" : "main");
            Directory.Exists(Path.Combine(working.FullName, ".git")).ShouldBeFalse();
        }
        finally
        {
            FileSystemHelper.Directory.DeleteDirectory(scratch.FullName);
        }
    }
}
