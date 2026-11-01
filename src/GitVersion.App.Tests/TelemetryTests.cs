using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http;
using GitVersion.Agents;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Telemetry;
using GitVersion.Testing.Extensions;
using GitVersion.Tests;

namespace GitVersion.App.Tests;

/// <summary>Verifies CLI telemetry argument projection and isolation from parsing and execution results.</summary>
[NonParallelizable]
public class TelemetryTests : TestBase
{
    private static readonly TelemetryCampaign Active = new("phc_testonly", "eu", "2026-10-01");

    /// <summary>Verifies the parsed POSIX opt-out bypasses sampling, disclosure, and transport.</summary>
    [Test]
    public async Task ParserOptOutPreventsAllTelemetryWork()
    {
        var sp = ConfigureServices(services => services.AddModule(new GitVersionAppModule()));
        var args = sp.GetRequiredService<IArgumentParser>().ParseArguments("--telemetry-opt-out");
        args.TelemetryOptOut.ShouldBeTrue();
        var calls = 0;
        await Reporter(args, random: () => throw new InvalidOperationException(), createClient: () =>
        {
            calls++;
            throw new InvalidOperationException();
        }, notice: _ => calls++).ReportAsync(CancellationToken.None);
        calls.ShouldBe(0);
    }

    /// <summary>Resolves production reporting without constructing runtime readers for an opted-out CLI invocation.</summary>
    [Test]
    public async Task TelemetryModuleDefersRuntimeContextForOptedOutCli()
    {
        var resolutions = 0;
        var sp = ConfigureServices(services =>
        {
            services.AddModule(new GitVersionAppModule(["--telemetry-opt-out"]));
            services.AddModule(new GitVersionTelemetryModule());
            services.AddSingleton<TelemetryRuntimeContextReader>(_ =>
            {
                resolutions++;
                throw new InvalidOperationException("Runtime metadata must remain deferred");
            });
        });

        var reporter = sp.GetRequiredService<ITelemetryReporter>();
        reporter.ShouldBeOfType<TelemetryReporter>();
        resolutions.ShouldBe(0);
        await reporter.ReportAsync(CancellationToken.None);
        resolutions.ShouldBe(0);
    }

    /// <summary>Verifies secrets and unknown values cannot escape through the payload envelope, dimensions, or options.</summary>
    [Test]
    public void PayloadContainsOnlyAllowlistedDimensionsAndPresenceFlags()
    {
        const string secret = "sensitive/path?token=password\u000c\ud800";
        var args = new Arguments
        {
            TargetPath = secret,
            TargetUrl = secret,
            TargetBranch = secret,
            CommitId = secret,
            ClonePath = secret,
            Format = secret,
            ShowVariable = secret,
            ConfigurationFile = secret,
            OutputFile = secret,
            LogFilePath = secret,
            OverrideConfiguration = new Dictionary<object, object?> { [secret] = secret },
            Authentication = new() { Username = secret, Password = secret, Token = secret },
            Output = new HashSet<OutputType> { OutputType.Json, (OutputType)999 },
            NoCache = true
        };
        var text = JsonSerializer.Serialize(TelemetryReporter.CreatePayload(TelemetryInvocationFactory.Create(args), "phc_testonly", 0.01, secret, false));
        text.ShouldNotContain("sensitive");
        using var json = JsonDocument.Parse(text);
        json.RootElement.EnumerateObject().Select(p => p.Name).Order().ShouldBe(new[] { "api_key", "distinct_id", "event", "properties" });
        json.RootElement.GetProperty("event").GetString().ShouldBe("gitversion_cli_invocation");
        var props = json.RootElement.GetProperty("properties");
        props.EnumerateObject().Select(p => p.Name).Order().ShouldBe(new[]
        {
            "$geoip_disable", "$ip", "$process_person_profile", "architecture", "ci", "container", "gitversion_version", "invocation", "options", "os", "os_distribution", "os_version",
            "sampling_probability", "schema_version"
        });
        props.GetProperty("$process_person_profile").GetBoolean().ShouldBeFalse();
        props.GetProperty("$geoip_disable").GetBoolean().ShouldBeTrue();
        props.GetProperty("$ip").GetString().ShouldBe("0.0.0.0");
        props.GetProperty("sampling_probability").GetDouble().ShouldBe(0.01);
        props.GetProperty("ci").GetString().ShouldBe("none");
        props.GetProperty("options").EnumerateArray().Select(x => x.GetString()).ShouldBe(new[]
        {
            "--no-cache", "--config", "--override-config", "--show-variable", "--format", "--url", "--branch", "--commit",
            "--dynamic-repo-location", "--log-file", "--output-file", "--output=json"
        });
    }

    /// <summary>Verifies informational and configuration-only commands bypass telemetry sampling.</summary>
    [TestCase("help")]
    [TestCase("version")]
    [TestCase("schema")]
    [TestCase("validate")]
    [TestCase("migrate")]
    [TestCase("query")]
    [TestCase("show-config")]
    public async Task InformationalAndConfigurationCommandsDoNotCollect(string command)
    {
        var args = new Arguments
        {
            IsHelp = command == "help",
            IsVersion = command == "version",
            IsCliSchema = command == "schema",
            ValidateConfiguration = command == "validate",
            IsConfigurationMigration = command == "migrate",
            ShowConfiguration = command == "show-config",
            ConfigurationPropertyPath = command == "query" ? "any" : null
        };
        var work = 0;
        await Reporter(args, random: () => { work++; return 0; }).ReportAsync(CancellationToken.None);
        work.ShouldBe(0);
    }

    /// <summary>Verifies sampled telemetry leaves JSON and single-variable output identical to a baseline invocation.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task EnabledTelemetryPreservesJsonAndSingleVariableOutput(bool variable)
    {
        using var repository = new EmptyRepositoryFixture();
        repository.Repository.MakeATaggedCommit("1.2.3");
        var baseline = await new ProgramFixture(repository.RepositoryPath).Run(variable ? "--show-variable FullSemVer" : "--output json");
        var program = new ProgramFixture(repository.RepositoryPath);
        var notice = new StringBuilder();
        var handler = new CaptureHandler();
        program.WithOverrides(services => services.AddSingleton<ITelemetryReporter>(sp => Reporter(
            sp.GetRequiredService<Arguments>(), createClient: () => new HttpClient(handler), notice: s => notice.Append(s))));
        var result = await program.Run(variable ? "--show-variable FullSemVer" : "--output json");
        result.ExitCode.ShouldBe(0);
        result.ExitCode.ShouldBe(baseline.ExitCode);
        result.Output.ShouldBe(baseline.Output);
        notice.ToString().ShouldContain("DO_NOT_TRACK=1");
        handler.Bodies.Count.ShouldBe(1);
        if (!variable)
        {
            JsonDocument.Parse(result.Output!).Dispose();
        }
    }

    /// <summary>Verifies an offline telemetry request preserves the calculated version and successful exit code.</summary>
    [Test]
    public async Task OfflineTelemetryPreservesCalculationOutputAndExitCode()
    {
        using var repository = new EmptyRepositoryFixture();
        repository.Repository.MakeATaggedCommit("1.2.3");
        var program = new ProgramFixture(repository.RepositoryPath);
        program.WithOverrides(services => services.AddSingleton<ITelemetryReporter>(sp => Reporter(
            sp.GetRequiredService<Arguments>(), createClient: () => new HttpClient(new FailureHandler(false)))));
        var result = await program.Run("--show-variable FullSemVer");
        result.ExitCode.ShouldBe(0);
        result.Output!.Trim().ShouldBe("1.2.3");
    }

    /// <summary>Verifies telemetry failure cannot replace either a successful or a failing executor exit code.</summary>
    [TestCase(0)]
    [TestCase(17)]
    public async Task TransportFailurePreservesExecutorExitCode(int exitCode)
    {
        var executor = new FixedExecutor(exitCode);
        var program = new ProgramFixture();
        program.WithOverrides(services =>
        {
            services.AddSingleton<IGitVersionExecutor>(executor);
            services.AddSingleton<ITelemetryReporter>(sp => Reporter(sp.GetRequiredService<Arguments>(),
                createClient: () => new HttpClient(new FailureHandler(false))));
        });
        var result = await program.Run("--show-variable FullSemVer");
        result.ExitCode.ShouldBe(exitCode);
        executor.Calls.ShouldBe(1);
    }

    /// <summary>Verifies the recursive opt-out is accepted by configuration commands and schema export.</summary>
    [Test]
    public void OptOutIsAcceptedWithConfigurationCommands()
    {
        var original = SysEnv.GetEnvironmentVariable("GITVERSION_CONFIGURATION_VERSION");
        try
        {
            SysEnv.SetEnvironmentVariable("GITVERSION_CONFIGURATION_VERSION", "v7");
            var sp = ConfigureServices(services => services.AddModule(new GitVersionAppModule()));
            var parser = sp.GetRequiredService<IArgumentParser>();
            var validation = parser.ParseArguments("config validate --telemetry-opt-out");
            validation.ValidateConfiguration.ShouldBeTrue();
            validation.ValidationError.ShouldBeNull();
            parser.ParseArguments("config get calculation.tag-prefix --telemetry-opt-out").ConfigurationPropertyPath.ShouldBe("calculation.tag-prefix");
            parser.ParseArguments("config migrate --telemetry-opt-out").IsConfigurationMigration.ShouldBeTrue();
            parser.ParseArguments("--telemetry-opt-out --cli-schema").IsCliSchema.ShouldBeTrue();
        }
        finally
        {
            SysEnv.SetEnvironmentVariable("GITVERSION_CONFIGURATION_VERSION", original);
        }
    }

    /// <summary>Creates a reporter with a fixed active date and injectable effects; the default transport never sends.</summary>
    private static TelemetryReporter Reporter(Arguments? args = null, TestEnvironment? environment = null,
        TelemetryCampaign? campaign = null, Func<double>? random = null, Func<HttpClient>? createClient = null, Action<string>? notice = null,
        Func<ICurrentBuildAgent>? buildAgent = null, Func<TelemetryRuntimeContext>? runtimeContext = null) =>
        new(TelemetryInvocationFactory.Create(args ?? new Arguments()), environment ?? new TestEnvironment(), campaign ?? Active,
            new FixedClock(), buildAgent ?? (() => new LocalBuild(new TestEnvironment(), NullLogger<LocalBuild>.Instance, new FileSystem())),
            random ?? (() => 0), createClient ?? (() => throw new InvalidOperationException("Unexpected transport")), notice ?? (_ => { }), runtimeContext);

    /// <summary>Keeps campaign eligibility independent of the machine's current date.</summary>
    private sealed class FixedClock : TimeProvider
    {
        /// <summary>Returns a UTC instant inside the test campaign's collection window.</summary>
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);
    }

    /// <summary>Counts executor calls while returning a chosen exit code without performing version calculation.</summary>
    private sealed class FixedExecutor(int exitCode) : IGitVersionExecutor
    {
        internal int Calls { get; private set; }
        /// <summary>Records one execution and returns the configured exit code, ignoring calculation options.</summary>
        [SuppressMessage("Major Code Smell", "S1172:Unused method parameters should be removed",
            Justification = "IGitVersionExecutor requires options; this test double deliberately returns a fixed exit code.")]
        public int Execute(GitVersionOptions gitVersionOptions)
        {
            Calls++;
            return exitCode;
        }
    }

    /// <summary>Captures outgoing request details and returns a chosen status without using the network.</summary>
    private sealed class CaptureHandler(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        internal List<string> Bodies { get; } = [];
        internal Uri? Endpoint { get; private set; }
        internal HttpMethod? Method { get; private set; }
        internal string? ContentType { get; private set; }
        /// <summary>Records the request endpoint, method, content type, and body before returning the configured response.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Endpoint = request.RequestUri;
            Method = request.Method;
            ContentType = request.Content!.Headers.ContentType!.MediaType;
            Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(status);
        }
    }

    /// <summary>Simulates either an immediate network error or a request stalled until cancellation.</summary>
    private sealed class FailureHandler(bool timeout) : HttpMessageHandler
    {
        internal CancellationToken Token { get; private set; }
        /// <summary>Throws an offline error or captures the cancellation token and waits for its deadline.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!timeout)
            {
                throw new HttpRequestException("Offline");
            }
            Token = cancellationToken;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
