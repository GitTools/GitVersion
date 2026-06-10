using Buildalyzer;

namespace GitVersion.MsBuild.Tests.Helpers;

public sealed class MsBuildExeFixtureResult(IDisposable fixture) : IDisposable
{
    public required IAnalyzerResults MsBuild { get; init; }
    public required string Output { get; init; }
    public required string ProjectPath { get; init; }
    public void Dispose() => fixture.Dispose();
}
