using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal abstract class BuildAgentBase(IEnvironment environment, ILogger logger, IFileSystem fileSystem) : ICurrentBuildAgent
{
    protected readonly ILogger Logger = logger.NotNull();
    protected readonly IEnvironment Environment = environment.NotNull();
    protected readonly IFileSystem FileSystem = fileSystem.NotNull();

    protected abstract string EnvironmentVariable { get; }

    public abstract string? SetBuildNumber(GitVersionVariables variables);
    public abstract string[] SetOutputVariables(string name, string? value);

    public virtual bool CanApplyToCurrentContext() => !Environment.GetEnvironmentVariable(EnvironmentVariable).IsNullOrEmpty();

    public virtual string? GetCurrentBranch(bool usingDynamicRepos) => null;

    public virtual bool IsDefault => false;
    public virtual bool PreventFetch() => true;
    public virtual bool ShouldCleanUpRemotes() => false;

    public virtual void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true)
    {
        if (updateBuildNumber)
        {
            writer($"Set Build Number for '{GetType().Name}'.");
            writer(SetBuildNumber(variables));
        }

        writer($"Set Output Variables for '{GetType().Name}'.");
        foreach (var buildParameter in SetOutputVariables(variables))
        {
            writer(buildParameter);
        }
    }

    protected IEnumerable<string> SetOutputVariables(GitVersionVariables variables)
    {
        var output = new List<string>();

        foreach (var (key, value) in variables)
        {
            output.AddRange(SetOutputVariables(key, value));
        }

        return output;
    }
}
