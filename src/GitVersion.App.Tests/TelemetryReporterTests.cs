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
        var reporter = new TelemetryReporter(new TestConsoleAdapter(output), new TestTelemetryNoticeState(), sink);
        var arguments = new Arguments
        {
            Telemetry = new CommandLineTelemetry("1.2.3", nameof(ArgumentParser), "gitversion", null, []),
            TelemetryOptOut = false
        };

        reporter.Report(arguments);
        reporter.Report(arguments);

        sink.Payloads.Count.ShouldBe(2);
        output.ToString().Split("Telemetry", StringSplitOptions.None).Length.ShouldBe(2);
    }

    [Test]
    public void ReportSkipsOptedOutInvocations()
    {
        var sink = new TestTelemetrySink();
        var reporter = new TelemetryReporter(new TestConsoleAdapter(new StringBuilder()), new TestTelemetryNoticeState(), sink);
        var arguments = new Arguments
        {
            Telemetry = new CommandLineTelemetry("1.2.3", nameof(ArgumentParser), "gitversion", null, []),
            TelemetryOptOut = true
        };

        reporter.Report(arguments);

        sink.Payloads.ShouldBeEmpty();
    }

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
}
