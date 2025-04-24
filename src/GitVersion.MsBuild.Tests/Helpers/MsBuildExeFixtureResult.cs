using Buildalyzer;

namespace GitVersion.MsBuild.Tests.Helpers;

public sealed class MsBuildExeFixtureResult(IDisposable fixture) : IDisposable
{
    public IAnalyzerResults MsBuild { get; init; }
    public string Output { get; init; }
    public string ProjectPath { get; init; }
    public void Dispose() => fixture.Dispose();
}
