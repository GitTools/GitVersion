using System;
using Buildalyzer;
using GitTools.Testing;

namespace GitVersionTask.Tests.Helpers
{
    public class MsBuildExeFixtureResult : IDisposable
    {
        private readonly RepositoryFixtureBase fixture;

        public MsBuildExeFixtureResult(RepositoryFixtureBase fixture)
        {
            this.fixture = fixture;
        }
        public IAnalyzerResults MsBuild { get; set; }
        public string Output { get; set; }
        public string ProjectPath { get; set; }
        public void Dispose()
        {
            fixture.Dispose();
        }
    }
}
