using Microsoft.Build.Framework;

namespace GitVersion.MsBuild.Tests.Helpers;

public sealed class MsBuildTaskFixtureResult<T>(IDisposable fixture) : IDisposable
    where T : ITask
{
    public bool Success { get; init; }

    public T Task { get; init; }

    public int Errors { get; init; }
    public int Warnings { get; set; }
    public int Messages { get; set; }
    public string Log { get; init; }

    public void Dispose() => fixture.Dispose();
}
