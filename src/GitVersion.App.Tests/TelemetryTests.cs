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

/// <summary>Verifies telemetry privacy, eligibility, disclosure, sampling, and isolation from CLI results.</summary>
[NonParallelizable]
public class TelemetryTests : TestBase
{
    private static readonly TelemetryCampaign Active = new("phc_testonly", "eu", "2026-10-01");

    /// <summary>Defers runtime metadata reads until collection policy, disclosure and sampling all permit sending.</summary>
    [TestCase("disabled")]
    [TestCase("optout")]
    [TestCase("help")]
    [TestCase("unsampled")]
    [TestCase("notice-failure")]
    public async Task RejectedInvocationDoesNotReadRuntimeMetadata(string reason)
    {
        var reads = 0;
        await Reporter(args: new Arguments { TelemetryOptOut = reason == "optout", IsHelp = reason == "help" },
            campaign: reason == "disabled" ? Active with { ProjectKey = null } : Active,
            random: () => reason == "unsampled" ? 0.9 : 0,
            notice: _ => { if (reason == "notice-failure") { throw new IOException(); } },
            runtimeContext: () => { reads++; return TelemetryRuntimeContext.Unknown; }).ReportAsync(CancellationToken.None);
        reads.ShouldBe(0);
    }

    /// <summary>Includes tool and runtime releases independently of the caller and CI provider.</summary>
    [TestCase("GitVersion.MsBuild", "msbuild", 0.0001)]
    [TestCase("gittools-actions", "gittools-actions", 0.0001)]
    [TestCase("private-wrapper", "direct", 0.01)]
    public async Task SelectedInvocationContainsRuntimeAndToolVersion(string caller, string invocation, double probability)
    {
        var env = new TestEnvironment();
        env.SetEnvironmentVariable("GITVERSION_INTERNAL_CALLER", caller);
        var handler = new CaptureHandler();
        var disclosed = false;
        await Reporter(environment: env, createClient: () => new HttpClient(handler), notice: _ => disclosed = true,
            runtimeContext: () =>
            {
                disclosed.ShouldBeTrue();
                return new("docker", "alpine", "3.22", "7.0.0-beta.1");
            }).ReportAsync(CancellationToken.None);
        using var json = JsonDocument.Parse(handler.Bodies.Single());
        var props = json.RootElement.GetProperty("properties");
        props.GetProperty("schema_version").GetInt32().ShouldBe(2);
        props.GetProperty("ci").GetString().ShouldBe("none");
        props.GetProperty("invocation").GetString().ShouldBe(invocation);
        props.GetProperty("container").GetString().ShouldBe("docker");
        props.GetProperty("os_distribution").GetString().ShouldBe("alpine");
        props.GetProperty("os_version").GetString().ShouldBe("3.22");
        props.GetProperty("gitversion_version").GetString().ShouldBe("7.0.0-beta.1");
        props.GetProperty("sampling_probability").GetDouble().ShouldBe(probability);
        props.TryGetProperty("sampling_weight", out _).ShouldBeFalse();
    }

    /// <summary>Verifies production DI writes policy disclosure to stderr without contaminating stdout.</summary>
    [Test]
    public void ProductionNoticeUsesStderrAndPreservesStdout()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("/notice/marker");
        fileSystem.Path.GetDirectoryName(Arg.Any<string>()).Returns("/notice");
        fileSystem.Directory.CreateDirectory(Arg.Any<string>()).Returns(_ => throw new UnauthorizedAccessException());
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var previousOut = Console.Out;
        var previousError = Console.Error;
        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);
            var services = ConfigureServices(services =>
            {
                services.AddModule(new GitVersionAppModule());
                services.AddSingleton(fileSystem);
            });
            services.GetRequiredService<TelemetryNotice>().Show("Policy: https://gitversion.net/docs/usage/cli/telemetry-policy");
            stdout.ToString().ShouldBeEmpty();
            stderr.ToString().ShouldBe("Policy: https://gitversion.net/docs/usage/cli/telemetry-policy" + System.Environment.NewLine);
        }
        finally
        {
            Console.SetOut(previousOut);
            Console.SetError(previousError);
        }
    }

    /// <summary>Verifies disclosure precedes a rejected sample and its empty marker suppresses later notices.</summary>
    [Test]
    public async Task FirstEligibleRunLinksPolicyBeforeSamplingAndLaterRunsRememberDisclosure()
    {
        using var state = new NoticeFixture();
        var notice = state.Create();
        var handler = new CaptureHandler();
        await Reporter(random: () =>
        {
            state.Messages.Count.ShouldBe(1);
            return 0.5;
        }, notice: notice.Show, createClient: () => new HttpClient(handler)).ReportAsync(CancellationToken.None);
        handler.Bodies.ShouldBeEmpty();
        state.Messages.Single().ShouldContain("https://gitversion.net/docs/usage/cli/telemetry-policy");
        state.Messages.Single().ShouldContain("DO_NOT_TRACK=1");
        var marker = Directory.GetFiles(state.Root, "*", SearchOption.AllDirectories).Single();
        Path.GetFileName(marker).ShouldBe("telemetry-policy-v2");
        new FileInfo(marker).Length.ShouldBe(0);

        await Reporter(notice: state.Create().Show, createClient: () => new HttpClient(handler))
            .ReportAsync(CancellationToken.None);
        state.Messages.Count.ShouldBe(1);
        handler.Bodies.Count.ShouldBe(1);
    }

    /// <summary>Verifies campaign disablement and explicit opt-out prevent disclosure-state and build-agent access.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task DisabledOrOptedOutRunsNeverAccessNoticeState(bool optedOut)
    {
        using var state = new NoticeFixture();
        var accesses = 0;
        var notice = state.Create(localData: () => { accesses++; return state.Root; });
        await Reporter(new Arguments { TelemetryOptOut = optedOut },
            campaign: Active with { ProjectKey = optedOut ? Active.ProjectKey : null }, notice: notice.Show,
            buildAgent: () => { accesses++; throw new InvalidOperationException(); }).ReportAsync(CancellationToken.None);
        accesses.ShouldBe(0);
        state.Messages.ShouldBeEmpty();
        Directory.Exists(state.Root).ShouldBeFalse();
    }

    /// <summary>Verifies unreadable or unwritable marker storage does not prevent disclosure or eligible reporting.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task UnavailableNoticeStateStillDisclosesAndAllowsCalculation(bool readFailure)
    {
        using var state = new NoticeFixture();
        // A regular file cannot contain a notice directory, simulating an unwritable home.
        await File.WriteAllTextAsync(state.Root, "unwritable directory");
        var notice = state.Create(localData: readFailure ? () => throw new UnauthorizedAccessException() : () => state.Root);
        var handler = new CaptureHandler();
        await Reporter(notice: notice.Show, createClient: () => new HttpClient(handler)).ReportAsync(CancellationToken.None);
        state.Messages.Count.ShouldBe(1);
        handler.Bodies.Count.ShouldBe(1);
        (await File.ReadAllTextAsync(state.Root)).ShouldBe("unwritable directory");
    }

    /// <summary>Verifies failed stderr disclosure prevents both the marker and the network event.</summary>
    [Test]
    public async Task FailedDisclosureDoesNotCreateMarkerOrSendAnEvent()
    {
        using var state = new NoticeFixture();
        var notice = state.Create(write: _ => throw new IOException("stderr closed"));
        var handler = new CaptureHandler();
        await Reporter(notice: notice.Show, createClient: () => new HttpClient(handler)).ReportAsync(CancellationToken.None);
        handler.Bodies.ShouldBeEmpty();
        Directory.Exists(state.Root).ShouldBeFalse();
    }

    /// <summary>Verifies concurrent disclosures safely create one empty marker and later runs stay quiet.</summary>
    [Test]
    public async Task ConcurrentFirstRunsCreateOneEmptyMarkerWithoutFailing()
    {
        using var state = new NoticeFixture();
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => state.Create().Show("policy"))));
        var files = Directory.GetFiles(state.Root, "*", SearchOption.AllDirectories);
        files.Length.ShouldBe(1);
        new FileInfo(files.Single()).Length.ShouldBe(0);
        state.Messages.Count.ShouldBeInRange(1, 20);
        var count = state.Messages.Count;
        state.Create().Show("policy");
        state.Messages.Count.ShouldBe(count);
    }

    /// <summary>Accepts a valid embedded public key, with the independent release-date gate still required.</summary>
    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("false", false)]
    [TestCase("invalid", false)]
    [TestCase("1", false)]
    [TestCase(" phc_testonly ", false)]
    [TestCase("phx_private", false)]
    [TestCase("phc_testonly", true)]
    public void EmbeddedProjectKeyControlsEnablement(string? value, bool enabled)
    {
        var metadata = new[] { new AssemblyMetadataAttribute("TelemetryProjectKey", value) };
        var actual = TelemetryCampaign.ReadProjectKey(metadata);
        actual.ShouldBe(value);
        (Active with { ProjectKey = actual }).IsActive(new DateOnly(2026, 10, 18)).ShouldBe(enabled);
        (Active with { ProjectKey = actual, StartDate = null }).IsActive(new DateOnly(2026, 10, 18)).ShouldBeFalse();
    }

    /// <summary>Rejects absent or duplicate project-key entries, including two identical valid keys.</summary>
    [TestCase(0)]
    [TestCase(2)]
    public void MissingOrDuplicateProjectKeyFailsClosed(int count)
    {
        var metadata = Enumerable.Repeat(new AssemblyMetadataAttribute("TelemetryProjectKey", "phc_testonly"), count)
            .Append(new AssemblyMetadataAttribute("RepositoryUrl", "phc_testonly"));
        var key = TelemetryCampaign.ReadProjectKey(metadata);
        key.ShouldBeNull();
        (Active with { ProjectKey = key }).IsActive(new DateOnly(2026, 10, 18)).ShouldBeFalse();
    }

    /// <summary>Verifies the embedded metadata value controls eligibility and invalid dates fail closed.</summary>
    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("invalid", false)]
    [TestCase("2026-10-01", true)]
    public void EmbeddedDateMetadataControlsCampaignWindow(string? value, bool active)
    {
        var metadata = new[] { new AssemblyMetadataAttribute("TelemetryCampaignStart", value) };
        var start = TelemetryCampaign.ReadStartDate(metadata);
        start.ShouldBe(value);
        (Active with { StartDate = start }).IsActive(new DateOnly(2026, 10, 18)).ShouldBe(active);
    }

    /// <summary>Verifies unrelated assembly metadata cannot supply a collection start date.</summary>
    [Test]
    public void MissingCampaignMetadataFailsClosed()
    {
        var metadata = new[] { new AssemblyMetadataAttribute("RepositoryUrl", "https://example.invalid") };
        var start = TelemetryCampaign.ReadStartDate(metadata);
        start.ShouldBeNull();
        (Active with { StartDate = start }).IsActive(new DateOnly(2026, 10, 18)).ShouldBeFalse();
    }

    /// <summary>Verifies ambiguous date metadata disables collection instead of selecting an arbitrary window.</summary>
    [Test]
    public void DuplicateCampaignMetadataFailsClosed()
    {
        var metadata = new[]
        {
            new AssemblyMetadataAttribute("TelemetryCampaignStart", "2026-10-01"),
            new AssemblyMetadataAttribute("TelemetryCampaignStart", "2026-11-01")
        };
        var start = TelemetryCampaign.ReadStartDate(metadata);
        start.ShouldBeNull();
        (Active with { StartDate = start }).IsActive(new DateOnly(2026, 10, 18)).ShouldBeFalse();
    }

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

    /// <summary>Verifies legacy-parser DI always resolves a no-op reporter, even with active campaign settings.</summary>
    [Test]
    public async Task LegacyParserNeverRegistersCollectionEvenWithAnActiveCampaign()
    {
        var environment = Substitute.For<IEnvironment>();
        var sp = ConfigureServices(services =>
        {
            services.AddModule(new GitVersionAppModule(useLegacyParser: true));
            services.AddSingleton(Active);
            services.AddSingleton(environment);
        });
        var reporter = sp.GetRequiredService<ITelemetryReporter>();
        reporter.ShouldBeOfType<DisabledTelemetryReporter>();
        await reporter.ReportAsync(CancellationToken.None);
        environment.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Verifies both opt-out environment variables bypass every deferred telemetry dependency.</summary>
    [TestCase("DO_NOT_TRACK", "1")]
    [TestCase("DO_NOT_TRACK", "true")]
    [TestCase("DO_NOT_TRACK", "TRUE")]
    [TestCase("GITVERSION_TELEMETRY_OPTOUT", "1")]
    [TestCase("GITVERSION_TELEMETRY_OPTOUT", "true")]
    [TestCase("GITVERSION_TELEMETRY_OPTOUT", "TrUe")]
    public async Task EnvironmentOptOutPreventsAllTelemetryWork(string name, string value)
    {
        var env = new TestEnvironment();
        env.SetEnvironmentVariable(name, value);
        var work = 0;
        await Reporter(environment: env, random: () => { work++; return 0; },
            notice: _ => work++, createClient: () => { work++; throw new InvalidOperationException(); },
            buildAgent: () => { work++; throw new InvalidOperationException(); })
            .ReportAsync(CancellationToken.None);
        work.ShouldBe(0);
    }

    /// <summary>Provides disabled, incomplete, untrusted, and out-of-range campaign configurations.</summary>
    private static IEnumerable<TestCaseData> InvalidCampaigns()
    {
        yield return new TestCaseData(new TelemetryCampaign(null, "eu", null)).SetName("DisabledUnconfiguredCampaignDoesNoWork");
        yield return new TestCaseData(Active with { ProjectKey = null }).SetName("MissingKeyDoesNoWork");
        yield return new TestCaseData(Active with { ProjectKey = "phx_private" }).SetName("ReadKeyRejected");
        yield return new TestCaseData(Active with { Region = "https://attacker.invalid" }).SetName("ArbitraryEndpointRejected");
        yield return new TestCaseData(Active with { StartDate = null }).SetName("MissingDateDoesNoWork");
        yield return new TestCaseData(Active with { StartDate = "invalid" }).SetName("InvalidDateDoesNoWork");
        yield return new TestCaseData(Active with { StartDate = "9999-12-31" }).SetName("OverflowDateDoesNoWork");
        yield return new TestCaseData(Active with { DirectProbability = double.NaN }).SetName("InvalidProbabilityDoesNoWork");
        yield return new TestCaseData(Active with { DirectProbability = 0.02 }).SetName("ExcessiveDirectProbabilityDoesNoWork");
        yield return new TestCaseData(Active with { AutomatedProbability = 0.001 }).SetName("ExcessiveAutomatedProbabilityDoesNoWork");
        yield return new TestCaseData(Active with { AutomatedProbability = -1 }).SetName("NegativeProbabilityDoesNoWork");
    }

    /// <summary>Verifies invalid campaign configurations perform no sampling, disclosure, agent resolution, or transport.</summary>
    [TestCaseSource(nameof(InvalidCampaigns))]
    public async Task InvalidCampaignDoesNoWork(object configuration)
    {
        var campaign = (TelemetryCampaign)configuration;
        var work = 0;
        await Reporter(campaign: campaign, random: () => { work++; return 0; }, notice: _ => work++,
            createClient: () => { work++; throw new InvalidOperationException(); },
            buildAgent: () => { work++; throw new InvalidOperationException(); }).ReportAsync(CancellationToken.None);
        work.ShouldBe(0);
    }

    /// <summary>Verifies eligibility immediately around the inclusive start and exclusive three-month end date.</summary>
    [TestCase("2026-09-30", false)]
    [TestCase("2026-10-01", true)]
    [TestCase("2026-12-31", true)]
    [TestCase("2027-01-01", false)]
    [TestCase("2027-01-02", false)]
    public void CampaignUsesInclusiveStartAndExclusiveThreeMonthCutoff(string today, bool expected) =>
        Active.IsActive(DateOnly.ParseExact(today, "yyyy-MM-dd", CultureInfo.InvariantCulture)).ShouldBe(expected);

    /// <summary>Verifies the sampling interval includes zero, excludes the threshold, and rejects invalid draws.</summary>
    [TestCase(0, true)]
    [TestCase(0.009999, true)]
    [TestCase(0.01, false)]
    [TestCase(0.5, false)]
    [TestCase(1, false)]
    [TestCase(-1, false)]
    [TestCase(double.NaN, false)]
    public async Task SamplingHasExactDeterministicBoundaries(double draw, bool sent)
    {
        var handler = new CaptureHandler();
        await Reporter(random: () => draw, createClient: () => new HttpClient(handler)).ReportAsync(CancellationToken.None);
        handler.Bodies.Count.ShouldBe(sent ? 1 : 0);
    }

    /// <summary>Verifies a zero sampling probability skips disclosure, random generation, and transport.</summary>
    [Test]
    public async Task ZeroProbabilitySkipsRandomNoticeAndTransport()
    {
        var work = 0;
        await Reporter(campaign: Active with { DirectProbability = 0 }, random: () => { work++; return 0; },
            notice: _ => work++, createClient: () => { work++; throw new InvalidOperationException(); })
            .ReportAsync(CancellationToken.None);
        work.ShouldBe(0);
    }

    /// <summary>Verifies real build-agent resolution and MSBuild markers select bounded labels and lower sampling.</summary>
    [TestCase("GITHUB_ACTIONS", "true", "github", false)]
    [TestCase("GITLAB_CI", "true", "gitlab", false)]
    [TestCase("TF_BUILD", "True", "azure", false)]
    [TestCase("JENKINS_URL", "https://private.example", "jenkins", false)]
    [TestCase("TEAMCITY_VERSION", "private", "teamcity", false)]
    [TestCase("GITVERSION_INTERNAL_CALLER", "GitVersion.MsBuild", "none", true)]
    [TestCase("GITVERSION_INTERNAL_CALLER", " gitversion-msbuild ", "none", true)]
    [TestCase("APPVEYOR", "True", "appveyor", false)]
    [TestCase("TRAVIS", "true", "travis", false)]
    [TestCase("BITBUCKET_WORKSPACE", "private-workspace", "bitbucket", false)]
    [TestCase("BUILDKITE", "true", "buildkite", false)]
    [TestCase("CODEBUILD_SOURCE_VERSION", "private-ref", "codebuild", false)]
    [TestCase("ContinuaCI.Version", "private-version", "continua", false)]
    [TestCase("DRONE", "true", "drone", false)]
    [TestCase("BuildRunner", "MyGet", "myget", false)]
    [TestCase("JB_SPACE_PROJECT_KEY", "private-project", "space", false)]
    public async Task AutomatedInvocationsUseLowerSamplingAndBoundedLabels(string name, string value, string ci, bool msbuild)
    {
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(name, value);
        if (name == "TRAVIS")
        {
            environment.SetEnvironmentVariable("CI", "true");
        }
        var services = ConfigureServices(services => services.AddSingleton<IEnvironment>(environment));
        ICurrentBuildAgent ResolveAgent() => services.GetRequiredService<ICurrentBuildAgent>();
        var rejected = new CaptureHandler();
        await Reporter(environment: environment, buildAgent: ResolveAgent, random: () => 0.0001, createClient: () => new HttpClient(rejected))
            .ReportAsync(CancellationToken.None);
        rejected.Bodies.ShouldBeEmpty();
        var accepted = new CaptureHandler();
        await Reporter(environment: environment, buildAgent: ResolveAgent, createClient: () => new HttpClient(accepted)).ReportAsync(CancellationToken.None);
        using var json = JsonDocument.Parse(accepted.Bodies.Single());
        var props = json.RootElement.GetProperty("properties");
        props.GetProperty("ci").GetString().ShouldBe(ci);
        props.GetProperty("invocation").GetString().ShouldBe(msbuild ? "msbuild" : "direct");
        props.GetProperty("sampling_probability").GetDouble().ShouldBe(0.0001);
        props.TryGetProperty("sampling_weight", out _).ShouldBeFalse();
        if (value != "1" && !value.Equals("true", StringComparison.OrdinalIgnoreCase) && !value.Equals(ci, StringComparison.OrdinalIgnoreCase))
        {
            accepted.Bodies.Single().ShouldNotContain(value);
        }
    }

    /// <summary>Verifies an unselected automated run leaves fresh notice storage and stderr untouched.</summary>
    [TestCase("ci")]
    [TestCase("msbuild")]
    [TestCase("unknown")]
    public async Task UnsampledAutomationDoesNotAccessNoticeStateOrWriteStderr(string mode)
    {
        using var state = new NoticeFixture();
        using var stderr = new StringWriter();
        var accesses = 0;
        var notice = state.Create(write: stderr.WriteLine, localData: () => { accesses++; return state.Root; });
        var handler = new CaptureHandler();
        await AutomatedReporter(mode, () => 0.0001, () => new HttpClient(handler), notice.Show)
            .ReportAsync(CancellationToken.None);
        accesses.ShouldBe(0);
        stderr.ToString().ShouldBeEmpty();
        Directory.Exists(state.Root).ShouldBeFalse();
        handler.Bodies.ShouldBeEmpty();
    }

    /// <summary>Verifies selected automation discloses before sending and reuses the empty disclosure marker.</summary>
    [TestCase("ci")]
    [TestCase("msbuild")]
    [TestCase("unknown")]
    public async Task SampledAutomationDisclosesBeforeSendingAndRemembersDisclosure(string mode)
    {
        using var state = new NoticeFixture();
        var handler = new CaptureHandler();
        await AutomatedReporter(mode, () =>
        {
            state.Messages.ShouldBeEmpty();
            Directory.Exists(state.Root).ShouldBeFalse();
            return 0;
        }, () =>
        {
            state.Messages.Single().ShouldContain("https://gitversion.net/docs/usage/cli/telemetry-policy");
            return new HttpClient(handler);
        }, state.Create().Show).ReportAsync(CancellationToken.None);
        handler.Bodies.Count.ShouldBe(1);
        var marker = Directory.GetFiles(state.Root, "*", SearchOption.AllDirectories).Single();
        new FileInfo(marker).Length.ShouldBe(0);

        await AutomatedReporter(mode, () => 0, () => new HttpClient(handler), state.Create().Show)
            .ReportAsync(CancellationToken.None);
        state.Messages.Count.ShouldBe(1);
        handler.Bodies.Count.ShouldBe(2);
    }

    /// <summary>Verifies selected automation cannot send when its required policy disclosure fails.</summary>
    [TestCase("ci")]
    [TestCase("msbuild")]
    [TestCase("unknown")]
    public async Task SampledAutomationWithFailedDisclosureDoesNotSend(string mode)
    {
        using var state = new NoticeFixture();
        var writes = 0;
        var notice = state.Create(write: _ => { writes++; throw new IOException("stderr closed"); });
        var handler = new CaptureHandler();
        await AutomatedReporter(mode, () => 0, () => new HttpClient(handler), notice.Show)
            .ReportAsync(CancellationToken.None);
        writes.ShouldBe(1);
        handler.Bodies.ShouldBeEmpty();
        Directory.Exists(state.Root).ShouldBeFalse();
    }

    /// <summary>Verifies all registered supported build agents map to the documented fixed telemetry labels.</summary>
    [Test]
    public void SupportedBuildAgentsHaveBoundedTelemetryLabels()
    {
        var expected = new Dictionary<string, string>
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
            ["TravisCi"] = "travis",
            ["LocalBuild"] = "none"
        };
        var services = ConfigureServices();
        var agents = services.GetServices<IBuildAgent>().ToArray();
        foreach (var (type, label) in expected)
        {
            var agent = agents.Single(agent => agent.GetType().Name == type);
            TelemetryReporter.ClassifyBuildAgent((ICurrentBuildAgent)agent).ShouldBe(label, type);
        }
    }

    /// <summary>Verifies unknown agents use the other label and automated rate without reading branch or tag data.</summary>
    [Test]
    public async Task UnknownBuildAgentsUseOtherAndAutomatedSampling()
    {
        var agent = Substitute.For<ICurrentBuildAgent>();
        agent.IsDefault.Returns(false);
        var handler = new CaptureHandler();
        await Reporter(buildAgent: () => agent, createClient: () => new HttpClient(handler)).ReportAsync(CancellationToken.None);
        using var json = JsonDocument.Parse(handler.Bodies.Single());
        var properties = json.RootElement.GetProperty("properties");
        properties.GetProperty("ci").GetString().ShouldBe("other");
        properties.GetProperty("sampling_probability").GetDouble().ShouldBe(0.0001);
        handler.Bodies.Single().ShouldNotContain(agent.GetType().Name);
        agent.DidNotReceive().GetCurrentBranch(Arg.Any<bool>());
        agent.DidNotReceive().GetCurrentTag();
        agent.DidNotReceive().WriteIntegration(Arg.Any<Action<string?>>(), Arg.Any<GitVersion.OutputVariables.GitVersionVariables>(), Arg.Any<bool>());
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
        var text = JsonSerializer.Serialize(TelemetryReporter.CreatePayload(args, "phc_testonly", 0.01, secret, false));
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

    /// <summary>Verifies repeated reporting sends exactly one JSON POST, regardless of the HTTP response status.</summary>
    [TestCase(HttpStatusCode.OK)]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.TooManyRequests)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.TemporaryRedirect)]
    public async Task TransportSendsOnceWithoutRetries(HttpStatusCode status)
    {
        var handler = new CaptureHandler(status);
        var reporter = Reporter(createClient: () => new HttpClient(handler));
        await reporter.ReportAsync(CancellationToken.None);
        await reporter.ReportAsync(CancellationToken.None);
        handler.Bodies.Count.ShouldBe(1);
        handler.Endpoint.ShouldBe(new Uri("https://eu.i.posthog.com/i/v0/e/"));
        handler.Method.ShouldBe(HttpMethod.Post);
        handler.ContentType.ShouldBe("application/json");
    }

    /// <summary>Verifies network failures are swallowed and a stalled request observes the cancellation deadline.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task OfflineAndTimeoutFailuresAreHarmlessAndBounded(bool automated)
    {
        TelemetryReporter CreateReporter(Func<HttpClient> client) => automated
            ? AutomatedReporter("github", () => 0, client, _ => { })
            : Reporter(createClient: client);

        await CreateReporter(() => new HttpClient(new FailureHandler(false))).ReportAsync(CancellationToken.None);
        var timer = Stopwatch.StartNew();
        var handler = new FailureHandler(true);
        await CreateReporter(() => new HttpClient(handler)).ReportAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(3));
        timer.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(3));
        handler.Token.IsCancellationRequested.ShouldBeTrue();
    }

    /// <summary>Allows a cold automated connection beyond the direct deadline while retaining the direct latency bound.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task ColdConnectionBudgetDependsOnInvocationContext(bool automated)
    {
        var handler = new DelayedResponseHandler();
        var reporter = automated
            ? AutomatedReporter("github", () => 0, () => new HttpClient(handler), _ => { })
            : Reporter(createClient: () => new HttpClient(handler));

        await reporter.ReportAsync(CancellationToken.None);

        handler.Completed.ShouldBe(automated);
        handler.Token.IsCancellationRequested.ShouldBe(!automated);
    }

    /// <summary>Caller cancellation interrupts the larger automated budget without waiting for its deadline.</summary>
    [Test]
    public async Task CallerCancellationInterruptsAutomatedTransport()
    {
        var handler = new DelayedResponseHandler();
        using var cancellation = new CancellationTokenSource();
        var reporter = AutomatedReporter("github", () => 0, () => new HttpClient(handler), _ => { });
        var report = reporter.ReportAsync(cancellation.Token);
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));

        await cancellation.CancelAsync();
        await report.WaitAsync(TimeSpan.FromSeconds(3));

        handler.Completed.ShouldBeFalse();
        handler.Token.IsCancellationRequested.ShouldBeTrue();
    }

    /// <summary>Verifies an already-cancelled invocation never reaches sampling.</summary>
    [Test]
    public async Task CancelledInvocationDoesNoWork()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var work = 0;
        await Reporter(random: () => { work++; return 0; }).ReportAsync(cancellation.Token);
        work.ShouldBe(0);
    }

    /// <summary>Verifies concurrent reporters each send once with distinct nonempty event-scoped UUIDs.</summary>
    [Test]
    public async Task ConcurrentInvocationsHaveIndependentEventIdentities()
    {
        var identities = await Task.WhenAll(Enumerable.Range(0, 25).Select(async _ =>
        {
            var handler = new CaptureHandler();
            var reporter = Reporter(createClient: () => new HttpClient(handler));
            await Task.WhenAll(reporter.ReportAsync(CancellationToken.None), reporter.ReportAsync(CancellationToken.None));
            handler.Bodies.Count.ShouldBe(1);
            using var json = JsonDocument.Parse(handler.Bodies.Single());
            var id = json.RootElement.GetProperty("distinct_id").GetString();
            Guid.TryParse(id, out var parsed).ShouldBeTrue();
            parsed.ShouldNotBe(Guid.Empty);
            return id;
        }));
        identities.Distinct().Count().ShouldBe(25);
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

    /// <summary>Creates an automated invocation using a real CI resolver, an MSBuild marker, or an unknown agent.</summary>
    private static TelemetryReporter AutomatedReporter(string mode, Func<double> random, Func<HttpClient> createClient, Action<string> notice)
    {
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(mode == "msbuild" ? "GITVERSION_INTERNAL_CALLER" : "GITHUB_ACTIONS",
            mode == "msbuild" ? "GitVersion.MsBuild" : "true");
        var services = ConfigureServices(services => services.AddSingleton<IEnvironment>(environment));
        var unknown = Substitute.For<ICurrentBuildAgent>();
        unknown.IsDefault.Returns(false);
        return Reporter(environment: environment, random: random, createClient: createClient, notice: notice,
            buildAgent: () => mode == "unknown" ? unknown : services.GetRequiredService<ICurrentBuildAgent>());
    }

    /// <summary>Creates a reporter with a fixed active date and injectable effects; the default transport never sends.</summary>
    private static TelemetryReporter Reporter(Arguments? args = null, TestEnvironment? environment = null,
        TelemetryCampaign? campaign = null, Func<double>? random = null, Func<HttpClient>? createClient = null, Action<string>? notice = null,
        Func<ICurrentBuildAgent>? buildAgent = null, Func<TelemetryRuntimeContext>? runtimeContext = null) =>
        new(args ?? new Arguments(), environment ?? new TestEnvironment(), campaign ?? Active,
            new FixedClock(), buildAgent ?? (() => new LocalBuild(new TestEnvironment(), NullLogger<LocalBuild>.Instance, new FileSystem())),
            random ?? (() => 0), createClient ?? (() => throw new InvalidOperationException("Unexpected transport")), notice ?? (_ => { }), runtimeContext);

    /// <summary>Keeps campaign eligibility independent of the machine's current date.</summary>
    private sealed class FixedClock : TimeProvider
    {
        /// <summary>Returns a UTC instant inside the test campaign's collection window.</summary>
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 18, 0, 0, 0, TimeSpan.Zero);
    }

    /// <summary>Owns isolated temporary disclosure state and captures notices from concurrent reporters.</summary>
    private sealed class NoticeFixture : IDisposable
    {
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "gitversion-notice-" + Guid.NewGuid());
        internal ConcurrentBag<string> Messages { get; } = [];
        /// <summary>Creates a notice using this fixture's directory and message sink unless explicitly overridden.</summary>
        internal TelemetryNotice Create(Action<string>? write = null, Func<string>? localData = null) =>
            new(new FileSystem(), localData ?? (() => Root), write ?? Messages.Add);

        /// <summary>Removes either the marker directory or the regular file used to simulate an unwritable home.</summary>
        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, true);
            }
            else if (File.Exists(Root))
            {
                File.Delete(Root);
            }
        }
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

    /// <summary>Models response latency between the direct and automated deadlines without external networking.</summary>
    private sealed class DelayedResponseHandler : HttpMessageHandler
    {
        internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal CancellationToken Token { get; private set; }
        internal bool Completed { get; private set; }

        /// <summary>Signals transport entry, then completes only when the request survives the simulated connection delay.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            Started.SetResult();
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            Completed = true;
            return new HttpResponseMessage(HttpStatusCode.OK);
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
