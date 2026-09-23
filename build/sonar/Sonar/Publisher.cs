using System.Net.Http.Headers;
using System.Text.Json;

namespace GitVersion.Sonar;

public static class Publisher
{
    private const string Project = "GitTools_GitVersion";

    public static async Task Prepare(string repository, string scanner, string bundle, string identityFile, long run, int attempt)
    {
        using var github = new GitHub(Required("GH_TOKEN"));
        var identity = await github.Resolve(run, attempt);
        await github.Download(identity, bundle + ".zip");
        SafeFiles.Unpack(bundle + ".zip", bundle);
        await Processes.Materialize(identity, repository);
        Handoff.Verify(repository, bundle, identity);
        Handoff.Write(identityFile, identity);
        await Begin(repository, scanner, identity, null);
        Handoff.Import(repository, bundle, identity);
        identity.EnsureCurrent(await github.Resolve(run, attempt));
        Console.WriteLine("Admission and anonymous preparation passed. No analysis submitted.");
    }

    public static async Task Publish(string repository, string scanner, string bundle, string identityFile)
    {
        var identity = Handoff.Read<RunIdentity>(identityFile);
        using var github = new GitHub(Required("GH_TOKEN"));
        identity.EnsureCurrent(await github.Resolve(identity.RunId, identity.Attempt));
        Handoff.Verify(repository, bundle, identity);
        if (await github.HasReceipt(identity))
        {
            await SaveReceipt(identity, identityFile);
            Console.WriteLine("This source run/attempt already has a verified publication receipt.");
            return;
        }
        var token = Required("SONAR_TOKEN");
        using var sonar = new HttpClient { BaseAddress = new Uri("https://sonarcloud.io/"), Timeout = TimeSpan.FromMinutes(1) }; // NOSONAR Fixed credential destination; never configurable by PR data.
        sonar.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        async Task<JsonElement> Get(string path)
        {
            using var response = await sonar.GetAsync(path);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.Clone();
        }
        var settings = await Get($"api/settings/values?component={Project}&keys=sonar.autoscan.enabled");
        SafeFiles.Require(settings.GetProperty("settings").EnumerateArray().Any(s => s.GetProperty("key").GetString() == "sonar.autoscan.enabled" && s.GetProperty("value").GetString() == "false"), "Automatic analysis must be disabled during an approved cutover/rehearsal");
        var selector = identity.PullRequest is int pr ? $"pullRequest={pr}" : $"branch={Uri.EscapeDataString(identity.Branch)}";
        await Begin(repository, scanner, identity, token);
        Handoff.Import(repository, bundle, identity);
        identity.EnsureCurrent(await github.Resolve(identity.RunId, identity.Attempt));
        var submittedAt = DateTimeOffset.UtcNow;
        Console.WriteLine(await Processes.Run(scanner, ["end", "/d:sonar.token=" + token], repository, token));
        var reports = Directory.GetFiles(Path.Combine(repository, ".sonarqube"), "report-task.txt", SearchOption.AllDirectories);
        SafeFiles.Require(reports.Length == 1, "Missing or ambiguous Sonar task receipt");
        var receipt = (await File.ReadAllLinesAsync(reports[0])).Select(l => l.Split('=', 2)).Where(p => p.Length == 2).ToDictionary(p => p[0], p => p[1]);
        SafeFiles.Require(receipt["projectKey"] == Project, "Wrong Sonar project receipt");
        var taskId = Uri.EscapeDataString(receipt["ceTaskId"]);
        var analysisId = await WaitForAnalysis(identity, taskId, Get);
        identity.EnsureCurrent(await github.Resolve(identity.RunId, identity.Attempt));
        var measures = await Get($"api/measures/component?component={Project}&{selector}&metricKeys=coverage,lines_to_cover");
        var values = measures.GetProperty("component").GetProperty("measures").EnumerateArray().ToDictionary(m => m.GetProperty("metric").GetString()!, m => m.GetProperty("value").GetString()!);
        SafeFiles.Require(values.ContainsKey("coverage") && values.TryGetValue("lines_to_cover", out var lines) && int.Parse(lines, System.Globalization.CultureInfo.InvariantCulture) > 0, "Sonar did not report imported coverage");
        var gate = (await Get("api/qualitygates/project_status?analysisId=" + Uri.EscapeDataString(analysisId))).GetProperty("projectStatus").GetProperty("status").GetString();
        Console.WriteLine($"Sonar analysis {analysisId}: coverage {values["coverage"]}%, quality gate {gate}.");
        if (identity.PullRequest is int number)
        {
            var decorated = false;
            for (var poll = 0; poll < 12 && !decorated; poll++)
            {
                var checks = await github.Get($"repos/{identity.Repository}/commits/{identity.HeadSha}/check-runs?per_page=100");
                var comments = await github.Get($"repos/{identity.Repository}/issues/{number}/comments?per_page=100");
                decorated = SonarResult.HasDecoration(checks, comments, identity, submittedAt);
                if (!decorated)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
            SafeFiles.Require(decorated, "Fresh Sonar check/comment missing on this PR head");
        }
        SafeFiles.Require(gate == "OK", "Sonar quality gate failed; Sonar's own PR result is authoritative");
        await SaveReceipt(identity, identityFile);
    }

    private static async Task<string> WaitForAnalysis(RunIdentity identity, string taskId, Func<string, Task<JsonElement>> get)
    {
        string? analysisId = null;
        for (var poll = 0; poll < 60; poll++)
        {
            var task = (await get("api/ce/task?id=" + taskId)).GetProperty("task");
            var state = task.GetProperty("status").GetString();
            SafeFiles.Require(state is not ("FAILED" or "CANCELED"), "Sonar compute task failed; inspect the Sonar activity page");
            SafeFiles.Require(task.GetProperty("componentKey").GetString() == Project && (identity.PullRequest is not int key || task.GetProperty("pullRequest").GetString() == key.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Sonar processed the wrong project or PR");
            if (state == "SUCCESS") { analysisId = task.GetProperty("analysisId").GetString(); break; }
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
        SafeFiles.Require(analysisId is not null, "Sonar processing timed out; inspect task before retrying");
        return analysisId!;
    }

    private static async Task SaveReceipt(RunIdentity identity, string identityFile)
    {
        Handoff.Write(identityFile + ".published.json", identity);
        await File.AppendAllTextAsync(Required("GITHUB_OUTPUT"), $"receipt={GitHub.ReceiptName(identity)}\n");
    }

    private static async Task Begin(string repository, string scanner, RunIdentity identity, string? token)
    {
        var configuration = Path.Combine(Path.GetDirectoryName(scanner)!, "trusted-analysis.xml");
        await File.WriteAllTextAsync(configuration, "<SonarQubeAnalysisProperties xmlns=\"http://www.sonarsource.com/msbuild/integration/2015/1\" />");
        var args = new List<string> { "begin", "/k:" + Project, "/o:gittools", "/s:" + configuration,
            "/d:sonar.host.url=https://sonarcloud.io", "/d:sonar.userHome=" + Path.Combine(Path.GetDirectoryName(scanner)!, "cache"),
            "/d:sonar.projectBaseDir=" + repository, "/d:sonar.scm.revision=" + identity.HeadSha,
            "/d:sonar.exclusions=build/common/Addins/**/*,.devagent/**", "/d:sonar.test.inclusions=**/*.Tests/**,**/GitVersion.Testing/**",
            "/d:sonar.cs.cobertura.reportsPaths=" + Path.Combine(repository, ".sonarqube/coverage/*.xml"),
            "/d:sonar.pullrequest.github.summary_comment=true" };
        if (identity.PullRequest is int pr)
        {
            args.AddRange([$"/d:sonar.pullrequest.key={pr}", "/d:sonar.pullrequest.branch=" + identity.Branch, "/d:sonar.pullrequest.base=" + identity.BaseBranch]);
        }
        else
        {
            args.Add("/d:sonar.branch.name=" + identity.Branch);
        }

        if (token is not null)
        {
            args.Add("/d:sonar.token=" + token);
        }

        Console.WriteLine(await Processes.Run(scanner, args, repository, token));
    }

    private static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : throw new InvalidDataException("Missing " + name);
}
