using Buildalyzer;

namespace GitVersion.MsBuild.Tests.Helpers;

public sealed class MsBuildExeFixtureResult(IDisposable fixture) : IDisposable
{
    public IAnalyzerResults MsBuild { get; set; }
    public string Output { get; set; }
    public string ProjectPath { get; set; }
    public void Dispose() => fixture.Dispose();
}
