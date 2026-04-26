using GitVersion.Extensions;

namespace GitVersion;

internal interface ITelemetryReleaseDateProvider
{
    bool TryGetReleaseDate(out DateOnly releaseDate);
}

internal interface ITelemetryUtcDateProvider
{
    DateOnly UtcToday { get; }
}

internal interface ITelemetrySink
{
    bool IsEnabled { get; }
    void Write(CommandLineTelemetry telemetry);
}

internal interface ITelemetryNoticeState
{
    bool HasSeenNotice();
    void MarkNoticeSeen();
}

internal interface ITelemetryReporter
{
    void Report(Arguments arguments);
}

internal interface ITelemetryContextEnricher
{
    CommandLineTelemetry Enrich(CommandLineTelemetry telemetry);
}

internal sealed class AssemblyTelemetryReleaseDateProvider : ITelemetryReleaseDateProvider
{
    public bool TryGetReleaseDate(out DateOnly releaseDate) =>
        TelemetryReleaseDate.TryGetReleaseDate(System.Reflection.Assembly.GetExecutingAssembly(), out releaseDate);
}

internal sealed class TelemetryUtcDateProvider : ITelemetryUtcDateProvider
{
    public DateOnly UtcToday => DateOnly.FromDateTime(DateTime.UtcNow);
}

internal sealed class NoOpTelemetrySink : ITelemetrySink
{
    public bool IsEnabled => false;

    public void Write(CommandLineTelemetry telemetry)
    {
    }
}

internal sealed class TelemetryContextEnricher(GitVersion.Agents.ICurrentBuildAgent currentBuildAgent, IEnvironment environment) : ITelemetryContextEnricher
{
    private readonly GitVersion.Agents.ICurrentBuildAgent currentBuildAgent = currentBuildAgent.NotNull();
    private readonly IEnvironment environment = environment.NotNull();

    public CommandLineTelemetry Enrich(CommandLineTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return telemetry with
        {
            ContinuousIntegrationProvider = ResolveContinuousIntegrationProvider(this.currentBuildAgent),
            InvocationSource = ResolveInvocationSource(this.environment)
        };
    }

    private static string ResolveContinuousIntegrationProvider(GitVersion.Agents.ICurrentBuildAgent currentBuildAgent) => currentBuildAgent.GetType().Name switch
    {
        "AppVeyor" => "appveyor",
        "AzurePipelines" => "azure-pipelines",
        "BitBucketPipelines" => "bitbucket-pipelines",
        "BuildKite" => "buildkite",
        "CodeBuild" => "codebuild",
        "ContinuaCi" => "continua-ci",
        "Drone" => "drone",
        "EnvRun" => "envrun",
        "GitHubActions" => "github-actions",
        "GitLabCi" => "gitlab-ci",
        "Jenkins" => "jenkins",
        "MyGet" => "myget",
        "SpaceAutomation" => "space-automation",
        "TeamCity" => "teamcity",
        "TravisCI" => "travis-ci",
        _ => TelemetryContextValues.Unknown
    };

    private static string ResolveInvocationSource(IEnvironment environment)
    {
        var caller = environment.GetEnvironmentVariable(TelemetryContextValues.InternalCallerEnvironmentVariable);
        return caller?.Trim() switch
        {
            { Length: > 0 } value when value.Equals("GitVersion.MsBuild", StringComparison.OrdinalIgnoreCase) => TelemetryContextValues.GitVersionMsBuild,
            { Length: > 0 } value when value.Equals(TelemetryContextValues.GitVersionMsBuild, StringComparison.OrdinalIgnoreCase) => TelemetryContextValues.GitVersionMsBuild,
            _ => TelemetryContextValues.Direct
        };
    }
}

internal sealed class FileTelemetryNoticeState(System.IO.Abstractions.IFileSystem fileSystem) : ITelemetryNoticeState
{
    private readonly System.IO.Abstractions.IFileSystem fileSystem = fileSystem.NotNull();

    public bool HasSeenNotice() => this.fileSystem.File.Exists(GetNoticeFilePath());

    public void MarkNoticeSeen()
    {
        var noticeFilePath = GetNoticeFilePath();
        var directoryPath = this.fileSystem.Path.GetDirectoryName(noticeFilePath).NotNull();
        this.fileSystem.Directory.CreateDirectory(directoryPath);

        if (!this.fileSystem.File.Exists(noticeFilePath))
        {
            this.fileSystem.File.WriteAllText(noticeFilePath, string.Empty);
        }
    }

    private string GetNoticeFilePath()
    {
        var localApplicationData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        return this.fileSystem.Path.Combine(localApplicationData, "GitVersion", "telemetry.notice");
    }
}

internal sealed class TelemetryReporter(
    IConsole console,
    ITelemetryNoticeState telemetryNoticeState,
    ITelemetrySink telemetrySink,
    ITelemetryContextEnricher telemetryContextEnricher,
    ITelemetryReleaseDateProvider telemetryReleaseDateProvider,
    ITelemetryUtcDateProvider telemetryUtcDateProvider
) : ITelemetryReporter
{
    private const string TelemetryNotice = """
        Telemetry
        ---------
        This GitVersion distribution can collect CLI usage telemetry to help inform OSS design decisions.
        The payload can include the command name, selected arguments, the GitVersion version, the parser implementation,
        the detected CI provider, and whether the CLI was invoked by GitVersion.MsBuild.
        Path values and sensitive argument values are redacted.
        You can opt out by setting DO_NOT_TRACK=1, GITVERSION_TELEMETRY_OPTOUT=1, or passing --telemetry-opt-out.
        Read more: https://gitversion.net/docs/usage/cli/telemetry
        """;

    private readonly IConsole console = console.NotNull();
    private readonly ITelemetryNoticeState telemetryNoticeState = telemetryNoticeState.NotNull();
    private readonly ITelemetrySink telemetrySink = telemetrySink.NotNull();
    private readonly ITelemetryContextEnricher telemetryContextEnricher = telemetryContextEnricher.NotNull();
    private readonly ITelemetryReleaseDateProvider telemetryReleaseDateProvider = telemetryReleaseDateProvider.NotNull();
    private readonly ITelemetryUtcDateProvider telemetryUtcDateProvider = telemetryUtcDateProvider.NotNull();

    public void Report(Arguments arguments)
    {
        if (arguments.TelemetryOptOut || arguments.Telemetry == null || !this.telemetrySink.IsEnabled)
        {
            return;
        }

        if (!this.telemetryReleaseDateProvider.TryGetReleaseDate(out var releaseDate)
            || !TelemetryReleaseDate.IsWithinWindow(releaseDate, this.telemetryUtcDateProvider.UtcToday))
        {
            return;
        }

        if (!this.telemetryNoticeState.HasSeenNotice())
        {
            this.console.WriteLine(TelemetryNotice);
            this.telemetryNoticeState.MarkNoticeSeen();
        }

        this.telemetrySink.Write(this.telemetryContextEnricher.Enrich(arguments.Telemetry));
    }
}
