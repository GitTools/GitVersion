using System;
using GitTools.Testing;
using Microsoft.Build.Framework;

namespace GitVersionTask.Tests.Helpers
{
    public class MsBuildTaskFixtureResult<T> : IDisposable where T : ITask
    {
        private readonly RepositoryFixtureBase fixture;

        public MsBuildTaskFixtureResult(RepositoryFixtureBase fixture)
        {
            this.fixture = fixture;
        }
        public bool Success { get; set; }

        public T Task { get; set; }

        public int Errors { get; set; }
        public int Warnings { get; set; }
        public int Messages { get; set; }
        public string Log { get; set; }

        public void Dispose()
        {
            fixture.Dispose();
        }
    }
}
