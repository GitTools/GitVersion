using GitVersion.Agents;

namespace GitVersion.MsBuild.Tests.Helpers;

/// <summary>
/// An exclusive scope over the *process-wide* environment-variable manipulation that the MsBuild
/// task and MsBuild.exe fixtures depend on. The MsBuild task detects the current build agent from
/// the real process environment, and the MsBuild.exe fixture launches a child process that inherits
/// it, so both are sensitive to the environment being mutated by another fixture.
/// </summary>
/// <remarks>
/// Under <c>[assembly: Parallelizable(ParallelScope.Fixtures)]</c> several fixtures run at the same
/// time. Without coordination, one fixture setting a CI variable such as <c>TF_BUILD</c> could leak
/// into another fixture's task or child process (making it wrongly detect a build agent), and one
/// fixture's reset could clear another's variables mid-execution. <see cref="Enter"/> takes an
/// exclusive lock, resets the build-agent variables to a clean baseline and applies the requested
/// variables; disposing the scope resets them again and releases the lock, so the whole window is
/// mutually exclusive:
/// <code>
/// using var environment = MsBuildProcessEnvironment.Enter(variables);
/// // ... run the task or build ...
/// </code>
/// </remarks>
internal sealed class MsBuildProcessEnvironment : IDisposable
{
    private static readonly object Lock = new();

    private static readonly string[] BuildAgentVariableNames =
    [
        TeamCity.EnvironmentVariableName,
        AppVeyor.EnvironmentVariableName,
        TravisCi.EnvironmentVariableName,
        Jenkins.EnvironmentVariableName,
        AzurePipelines.EnvironmentVariableName,
        GitHubActions.EnvironmentVariableName,
        SpaceAutomation.EnvironmentVariableName
    ];

    private MsBuildProcessEnvironment(KeyValuePair<string, string?>[]? environmentVariables)
    {
        Monitor.Enter(Lock);
        Reset();
        Apply(environmentVariables);
    }

    /// <summary>
    /// Acquires exclusive access to the process environment, resets the build-agent variables to a
    /// clean, non-CI baseline and applies <paramref name="environmentVariables"/>. Dispose the
    /// returned scope to reset again and release the lock.
    /// </summary>
    public static MsBuildProcessEnvironment Enter(KeyValuePair<string, string?>[]? environmentVariables = null) =>
        new(environmentVariables);

    public void Dispose()
    {
        Reset();
        Monitor.Exit(Lock);
    }

    private static void Apply(KeyValuePair<string, string?>[]? environmentVariables)
    {
        if (environmentVariables == null)
        {
            return;
        }

        foreach (var (key, value) in environmentVariables)
        {
            SysEnv.SetEnvironmentVariable(key, value);
        }
    }

    private static void Reset()
    {
        foreach (var name in BuildAgentVariableNames)
        {
            SysEnv.SetEnvironmentVariable(name, null);
        }
    }
}
