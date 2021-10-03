using Buildalyzer;
using GitTools.Testing;

namespace GitVersion.MsBuild.Tests.Helpers;

public sealed class MsBuildExeFixtureResult : IDisposable
{
    private readonly RepositoryFixtureBase fixture;

    public MsBuildExeFixtureResult(RepositoryFixtureBase fixture) => this.fixture = fixture;
    public IAnalyzerResults MsBuild { get; set; }
    public string Output { get; set; }
    public string ProjectPath { get; set; }
    public void Dispose() => this.fixture.Dispose();
}
