using System.Text.Json;

namespace GitVersion.Sonar;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            switch (args)
            {
                case ["project-ids", var repository, var destination]:
                    ProjectIds.Write(Path.GetFullPath(repository), destination);
                    break;
                case ["identity", var output]:
                    Handoff.Write(output, EnvironmentIdentity());
                    break;
                case ["resolve", var run, var attempt]:
                    using (var github = new GitHub(Environment.GetEnvironmentVariable("GH_TOKEN") ?? throw new InvalidDataException("Missing GH_TOKEN")))
                    {
                        var resolved = await github.Resolve(long.Parse(run), int.Parse(attempt));
                        var key = resolved.PullRequest is int pr ? $"pr-{pr}" : "branch-" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(resolved.Branch)));
                        await File.AppendAllTextAsync(Environment.GetEnvironmentVariable("GITHUB_OUTPUT") ?? throw new InvalidDataException("Missing GITHUB_OUTPUT"), $"key={key}\n");
                    }
                    break;
                case ["collect", var repository, var scanner, var coverage, var bundle, var identity]:
                    Handoff.Collect(Path.GetFullPath(repository), Path.GetFullPath(scanner), coverage, bundle, Handoff.Read<RunIdentity>(identity));
                    break;
                case ["verify", var repository, var bundle, var identity]:
                    Console.WriteLine(JsonSerializer.Serialize(Handoff.Verify(Path.GetFullPath(repository), bundle, Handoff.Read<RunIdentity>(identity))));
                    break;
                case ["admit", var repository, var scanner, var bundle, var identity, var run, var attempt]:
                    await Publisher.Prepare(repository, scanner, bundle, identity, long.Parse(run), int.Parse(attempt));
                    break;
                case ["publish", var repository, var scanner, var bundle, var identity]:
                    await Publisher.Publish(repository, scanner, bundle, identity);
                    break;
                default:
                    throw new InvalidDataException("Expected project-ids, identity, collect, verify, admit or publish; see .github/sonar.md");
            }
            return 0;
        }
        catch (Exception error)
        {
            await Console.Error.WriteLineAsync(error.Message);
            return 1;
        }
    }

    private static RunIdentity EnvironmentIdentity()
    {
        static string Env(string key) => Environment.GetEnvironmentVariable(key) ?? throw new InvalidDataException("Missing " + key);
        var isPr = Env("GITHUB_EVENT_NAME") == "pull_request";
        var identity = new RunIdentity(Env("GITHUB_REPOSITORY"), long.Parse(Env("GITHUB_RUN_ID")), int.Parse(Env("GITHUB_RUN_ATTEMPT")), Env("GITHUB_EVENT_NAME"),
            isPr ? Env("SONAR_HEAD_SHA") : Env("GITHUB_SHA"), Env("GITHUB_SHA"), isPr ? Env("SONAR_BASE_SHA") : Env("GITHUB_SHA"),
            isPr ? int.Parse(Env("SONAR_PR")) : null, isPr ? Env("GITHUB_HEAD_REF") : Env("GITHUB_REF_NAME"), isPr ? Env("GITHUB_BASE_REF") : Env("GITHUB_REF_NAME"));
        identity.Validate();
        return identity;
    }
}
