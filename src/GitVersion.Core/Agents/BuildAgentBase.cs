using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal abstract class BuildAgentBase(IEnvironment environment, ILog log) : ICurrentBuildAgent
{
    protected readonly ILog Log = log.NotNull();
    protected IEnvironment Environment { get; } = environment.NotNull();

    protected abstract string EnvironmentVariable { get; }
    public virtual bool IsDefault => false;

    public abstract string? GenerateSetVersionMessage(GitVersionVariables variables);
    public abstract string[] GenerateSetParameterMessage(string name, string? value);

    public virtual bool CanApplyToCurrentContext() => !Environment.GetEnvironmentVariable(EnvironmentVariable).IsNullOrEmpty();

    public virtual string? GetCurrentBranch(bool usingDynamicRepos) => null;

    public virtual bool PreventFetch() => true;
    public virtual bool ShouldCleanUpRemotes() => false;

    public virtual void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true)
    {
        if (updateBuildNumber)
        {
            writer($"Executing GenerateSetVersionMessage for '{GetType().Name}'.");
            writer(GenerateSetVersionMessage(variables));
        }
        writer($"Executing GenerateBuildLogOutput for '{GetType().Name}'.");
        foreach (var buildParameter in GenerateBuildLogOutput(variables))
        {
            writer(buildParameter);
        }
    }

    protected IEnumerable<string> GenerateBuildLogOutput(GitVersionVariables variables)
    {
        var output = new List<string>();

        foreach (var (key, value) in variables)
        {
            output.AddRange(GenerateSetParameterMessage(key, value));
        }

        return output;
    }
}
