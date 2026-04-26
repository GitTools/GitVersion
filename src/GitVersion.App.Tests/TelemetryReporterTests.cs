using GitVersion.Core.Tests.Helpers;

namespace GitVersion.App.Tests;

[TestFixture]
public class TelemetryReporterTests
{
    [Test]
    public void ReportWritesNoticeOnceAndEmitsTelemetryWhenSinkIsEnabled()
    {
        var output = new StringBuilder();
        var sink = new TestTelemetrySink();
        var reporter = new TelemetryReporter(
            new TestConsoleAdapter(output), new TestTelemetryNoticeState(), sink, new TestTelemetryContextEnricher("github-actions", TelemetryContextValues.GitVersionMsBuild),
            new TestTelemetryReleaseDateProvider(true, new(2026, 04, 26)),
            new TestTelemetryUtcDateProvider(new(2026, 04, 26)));
        var arguments = new Arguments
        {
            Telemetry = CreateTelemetry(),
            TelemetryOptOut = false
        };

        reporter.Report(arguments);
        reporter.Report(arguments);

        sink.Payloads.Count.ShouldBe(2);
        sink.Payloads[0].ContinuousIntegrationProvider.ShouldBe("github-actions");
        sink.Payloads[0].InvocationSource.ShouldBe(TelemetryContextValues.GitVersionMsBuild);
        sink.Payloads[1].ContinuousIntegrationProvider.ShouldBe("github-actions");
        sink.Payloads[1].InvocationSource.ShouldBe(TelemetryContextValues.GitVersionMsBuild);
        output.ToString().Split("Telemetry", StringSplitOptions.None).Length.ShouldBe(2);
    }

    [Test]
    public void ReportSkipsOptedOutInvocations()
    {
        var sink = new TestTelemetrySink();
        var reporter = new TelemetryReporter(
            new TestConsoleAdapter(new StringBuilder()), new TestTelemetryNoticeState(), sink, new TestTelemetryContextEnricher(),
            new TestTelemetryReleaseDateProvider(true, new(2026, 04, 26)),
            new TestTelemetryUtcDateProvider(new(2026, 04, 26)));
        var arguments = new Arguments
        {
            Telemetry = CreateTelemetry(),
            TelemetryOptOut = true
        };

        reporter.Report(arguments);

        sink.Payloads.ShouldBeEmpty();
    }

    [Test]
    public void ReportSkipsTelemetryWhenReleaseDateIsMissing()
    {
        var sink = new TestTelemetrySink();
        var reporter = new TelemetryReporter(
            new TestConsoleAdapter(new StringBuilder()), new TestTelemetryNoticeState(), sink, new TestTelemetryContextEnricher(),
            new TestTelemetryReleaseDateProvider(false, default),
            new TestTelemetryUtcDateProvider(new(2026, 04, 26)));
        var arguments = new Arguments
        {
            Telemetry = CreateTelemetry(),
            TelemetryOptOut = false
        };

        reporter.Report(arguments);

        sink.Payloads.ShouldBeEmpty();
    }

    [Test]
    public void ReportSkipsTelemetryWhenReleaseWindowHasExpired()
    {
        var sink = new TestTelemetrySink();
        var reporter = new TelemetryReporter(
            new TestConsoleAdapter(new StringBuilder()), new TestTelemetryNoticeState(), sink, new TestTelemetryContextEnricher(),
            new TestTelemetryReleaseDateProvider(true, new(2026, 01, 01)),
            new TestTelemetryUtcDateProvider(new(2026, 04, 01)));
        var arguments = new Arguments
        {
            Telemetry = CreateTelemetry(),
            TelemetryOptOut = false
        };

        reporter.Report(arguments);

        sink.Payloads.ShouldBeEmpty();
    }

    [Test]
    public void ReportDefaultsInvocationSourceToDirectWhenNoCallerIsDetected()
    {
        var sink = new TestTelemetrySink();
        var reporter = new TelemetryReporter(
            new TestConsoleAdapter(new StringBuilder()), new TestTelemetryNoticeState(), sink, new TestTelemetryContextEnricher(),
            new TestTelemetryReleaseDateProvider(true, new(2026, 04, 26)),
            new TestTelemetryUtcDateProvider(new(2026, 04, 26)));
        var arguments = new Arguments
        {
            Telemetry = CreateTelemetry(),
            TelemetryOptOut = false
        };

        reporter.Report(arguments);

        sink.Payloads.Single().ContinuousIntegrationProvider.ShouldBe(TelemetryContextValues.Unknown);
        sink.Payloads.Single().InvocationSource.ShouldBe(TelemetryContextValues.Direct);
    }

    private static CommandLineTelemetry CreateTelemetry() => new(
        "1.2.3",
        nameof(ArgumentParser),
        TelemetryContextValues.Unknown,
        TelemetryContextValues.Direct,
        "gitversion",
        null,
        []);

    private sealed class TestTelemetrySink : ITelemetrySink
    {
        public bool IsEnabled => true;

        public List<CommandLineTelemetry> Payloads { get; } = [];

        public void Write(CommandLineTelemetry telemetry) => Payloads.Add(telemetry);
    }

    private sealed class TestTelemetryNoticeState : ITelemetryNoticeState
    {
        private bool hasSeenNotice;

        public bool HasSeenNotice() => this.hasSeenNotice;

        public void MarkNoticeSeen() => this.hasSeenNotice = true;
    }

    private sealed class TestTelemetryReleaseDateProvider(bool hasReleaseDate, DateOnly releaseDate) : ITelemetryReleaseDateProvider
    {
        public bool TryGetReleaseDate(out DateOnly value)
        {
            value = releaseDate;
            return hasReleaseDate;
        }
    }

    private sealed class TestTelemetryContextEnricher(
        string continuousIntegrationProvider = TelemetryContextValues.Unknown,
        string invocationSource = TelemetryContextValues.Direct
    ) : ITelemetryContextEnricher
    {
        public CommandLineTelemetry Enrich(CommandLineTelemetry telemetry) => telemetry with
        {
            ContinuousIntegrationProvider = continuousIntegrationProvider,
            InvocationSource = invocationSource
        };
    }

    private sealed class TestTelemetryUtcDateProvider(DateOnly utcToday) : ITelemetryUtcDateProvider
    {
        public DateOnly UtcToday { get; } = utcToday;
    }
}
