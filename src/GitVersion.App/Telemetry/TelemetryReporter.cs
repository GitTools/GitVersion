using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.InteropServices;
using GitVersion.Agents;

namespace GitVersion.Telemetry;

/// <summary>Provides best-effort invocation reporting without affecting normal CLI results.</summary>
internal interface ITelemetryReporter
{
    /// <summary>Completes policy-controlled reporting before the caller proceeds with execution.</summary>
    /// <param name="cancellationToken">Cancels any reporting work for the invocation.</param>
    Task ReportAsync(CancellationToken cancellationToken);
}

/// <summary>Ensures unsupported entry points perform no telemetry work.</summary>
internal sealed class DisabledTelemetryReporter : ITelemetryReporter
{
    /// <summary>Returns immediately without reading environment, disclosure state, or network resources.</summary>
    public Task ReportAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Discloses and samples eligible invocations, then awaits at most one bounded PostHog request.</summary>
[method: SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
    Justification = "Explicit dependencies keep policy, clock, sampling, build-agent resolution, disclosure and transport independently testable.")]
internal sealed class TelemetryReporter(
    Arguments arguments,
    IEnvironment environment,
    TelemetryCampaign campaign,
    TimeProvider clock,
    Func<ICurrentBuildAgent> buildAgent,
    Func<double> random,
    Func<HttpClient> createClient,
    Action<string> notice,
    Func<TelemetryRuntimeContext>? runtimeContext = null) : ITelemetryReporter
{
    private const string OtherLabel = "other";
    private const string PolicyNotice = "GitVersion may send sampled CLI usage data to PostHog Cloud. Opt out with --telemetry-opt-out or DO_NOT_TRACK=1. Policy and collected data: https://gitversion.net/docs/usage/cli/telemetry-policy";
    internal static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DirectSendTimeout = TimeSpan.FromMilliseconds(300);
    private int attempted;
    private static readonly IReadOnlyDictionary<string, string> AgentLabels = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["AppVeyor"] = "appveyor",
        ["AzurePipelines"] = "azure",
        ["BitBucketPipelines"] = "bitbucket",
        ["BuildKite"] = "buildkite",
        ["CodeBuild"] = "codebuild",
        ["ContinuaCi"] = "continua",
        ["Drone"] = "drone",
        ["EnvRun"] = "envrun",
        ["GitHubActions"] = "github",
        ["GitLabCi"] = "gitlab",
        ["Jenkins"] = "jenkins",
        ["MyGet"] = "myget",
        ["SpaceAutomation"] = "space",
        ["TeamCity"] = "teamcity",
        ["TravisCi"] = "travis"
    };

    /// <summary>Checks opt-outs and campaign policy before disclosure, sampling, or a single transport attempt.</summary>
    /// <param name="cancellationToken">Cancels the request in addition to the transport's own deadline.</param>
    /// <remarks>Automation discloses only selected samples. All failures are isolated; each reporter can send at most once.</remarks>
    public async Task ReportAsync(CancellationToken cancellationToken)
    {
        // Everything, including policy/environment/notice failures, is best effort.
        try
        {
            if (Interlocked.Exchange(ref this.attempted, 1) != 0 || cancellationToken.IsCancellationRequested ||
                arguments.TelemetryOptOut || IsTrue("DO_NOT_TRACK") || IsTrue("GITVERSION_TELEMETRY_OPTOUT") ||
                !campaign.IsActive(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime)) ||
                arguments.IsHelp || arguments.IsVersion || arguments.IsCliSchema || arguments.ValidateConfiguration ||
                arguments.IsConfigurationMigration || arguments.ShowConfiguration || arguments.ConfigurationPropertyPath != null)
            {
                return;
            }

            var ci = ClassifyBuildAgent(buildAgent());
            var caller = environment.GetEnvironmentVariable("GITVERSION_INTERNAL_CALLER")?.Trim();
            var msbuild = string.Equals(caller, "GitVersion.MsBuild", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(caller, "gitversion-msbuild", StringComparison.OrdinalIgnoreCase);
            var actions = string.Equals(caller, "gittools-actions", StringComparison.OrdinalIgnoreCase);
            var automated = ci != "none" || msbuild || actions;
            var probability = automated ? campaign.AutomatedProbability : campaign.DirectProbability;
            if (probability <= 0)
            {
                return;
            }
            // Direct runs disclose before sampling; ephemeral CI runs stay quiet unless selected.
            if (!automated)
            {
                notice(PolicyNotice);
            }
            var draw = random();
            if (!double.IsFinite(draw) || draw < 0 || draw >= probability)
            {
                return;
            }
            if (automated)
            {
                notice(PolicyNotice);
            }

            var payload = CreatePayload(arguments, campaign.ProjectKey!, probability, ci, msbuild,
                runtimeContext?.Invoke(), actions);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(automated ? SendTimeout : DirectSendTimeout);
            using var client = createClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, campaign.Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            // Await one send before shutdown; don't read a response body, retry or queue to disk.
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .WaitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Telemetry must never change normal execution, stdout or exit code.
            return;
        }
    }

    /// <summary>Recognizes an opt-out environment value of 1 or case-insensitive true.</summary>
    private bool IsTrue(string name) => environment.GetEnvironmentVariable(name) is { } value &&
        (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));

    /// <summary>Maps the resolved build agent to a bounded CI label without querying repository information.</summary>
    internal static string ClassifyBuildAgent(ICurrentBuildAgent agent) => agent.IsDefault
        ? "none"
        : AgentLabels.GetValueOrDefault(agent.GetType().Name, OtherLabel);

    /// <summary>Constructs an allowlisted event with a fresh identity, bounded dimensions, and option-presence flags.</summary>
    /// <remarks>Raw arguments and option values are never copied into the payload.</remarks>
    internal static object CreatePayload(Arguments args, string projectKey, double probability, string ci, bool msbuild,
        TelemetryRuntimeContext? context = null, bool actions = false) => new
        {
            api_key = projectKey,
            @event = "gitversion_cli_invocation",
            distinct_id = Guid.NewGuid().ToString("D"),
            properties = new Dictionary<string, object>
            {
                ["$process_person_profile"] = false,
                ["$geoip_disable"] = true,
                ["$ip"] = "0.0.0.0",
                ["schema_version"] = 2,
                ["sampling_probability"] = probability,
                ["ci"] = ci == OtherLabel || AgentLabels.Values.Contains(ci, StringComparer.Ordinal) ? ci : "none",
                ["invocation"] = (msbuild, actions) switch
                {
                    (true, _) => "msbuild",
                    (_, true) => "gittools-actions",
                    _ => "direct"
                },
                ["container"] = (context ?? TelemetryRuntimeContext.Unknown).Container,
                ["os_distribution"] = (context ?? TelemetryRuntimeContext.Unknown).Distribution,
                ["os_version"] = (context ?? TelemetryRuntimeContext.Unknown).OsVersion,
                ["gitversion_version"] = (context ?? TelemetryRuntimeContext.Unknown).GitVersionVersion,
                ["os"] = ClassifyOperatingSystem(),
                ["architecture"] = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => "x64",
                    Architecture.Arm64 => "arm64",
                    Architecture.X86 => "x86",
                    Architecture.Arm => "arm",
                    _ => OtherLabel
                },
                ["options"] = CanonicalOptions(args)
            }
        };

    /// <summary>Returns a fixed OS family label without including host or distribution details.</summary>
    internal static string ClassifyOperatingSystem()
    {
        if (OperatingSystem.IsWindows())
        {
            return "windows";
        }
        if (OperatingSystem.IsLinux())
        {
            return "linux";
        }
        return OperatingSystem.IsMacOS() ? "macos" : OtherLabel;
    }

    /// <summary>Extracts canonical option names and allowlisted output modes while discarding free-form values.</summary>
    private static string[] CanonicalOptions(Arguments args)
    {
        var flags = new List<string>();
        Add(args.NoCache, "--no-cache");
        Add(args.NoFetch, "--no-fetch");
        Add(args.NoNormalize, "--no-normalize");
        Add(args.AllowShallow, "--allow-shallow");
        Add(args.Diag, "--diagnose");
        Add(args.ConfigurationFile != null, "--config");
        Add(args.OverrideConfiguration.Count > 0, "--override-config");
        Add(args.ShowVariable != null, "--show-variable");
        Add(args.Format != null, "--format");
        Add(args.TargetUrl != null, "--url");
        Add(args.TargetBranch != null, "--branch");
        Add(args.CommitId != null, "--commit");
        Add(args.ClonePath != null, "--dynamic-repo-location");
        Add(args.LogFilePath != null, "--log-file");
        Add(args.OutputFile != null, "--output-file");
        Add(args.UpdateAssemblyInfo, "--update-assembly-info");
        Add(args.UpdateProjectFiles, "--update-project-files");
        Add(args.EnsureAssemblyInfo, "--ensure-assembly-info");
        Add(args.UpdateWixVersionFile, "--update-wix-version-file");
        foreach (var output in args.Output.Order())
        {
            var name = output switch
            {
                OutputType.Json => "json",
                OutputType.BuildServer => "buildserver",
                OutputType.File => "file",
                OutputType.DotEnv => "dotenv",
                _ => null
            };
            if (name != null)
            {
                flags.Add("--output=" + name);
            }
        }
        return [.. flags];

        void Add(bool present, string name)
        {
            if (present)
            {
                flags.Add(name);
            }
        }
    }

    /// <summary>Creates a short-lived client with a bounded timeout and no redirects, cookies, or credentials.</summary>
    internal static HttpClient CreateClient() => new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        Credentials = null,
        ConnectTimeout = SendTimeout
    })
    { Timeout = SendTimeout };
}
