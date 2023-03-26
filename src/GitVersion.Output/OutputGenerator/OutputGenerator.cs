using GitVersion.Agents;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion.Output.OutputGenerator;

public interface IOutputGenerator : IVersionConverter<OutputContext>
{
}

public sealed class OutputGenerator : IOutputGenerator
{
    private readonly IConsole console;
    private readonly IFileSystem fileSystem;
    private readonly IEnvironment environment;
    private readonly IOptions<GitVersionOptions> options;
    private readonly ICurrentBuildAgent buildAgent;

    public OutputGenerator(ICurrentBuildAgent buildAgent, IConsole console, IFileSystem fileSystem, IEnvironment environment, IOptions<GitVersionOptions> options)
    {
        this.console = console.NotNull();
        this.fileSystem = fileSystem.NotNull();
        this.environment = environment;
        this.options = options.NotNull();
        this.buildAgent = buildAgent.NotNull();
    }

    public void Execute(VersionVariables variables, OutputContext context)
    {
        var gitVersionOptions = this.options.Value;
        if (gitVersionOptions.Output.Contains(OutputType.BuildServer))
        {
            this.buildAgent.WriteIntegration(this.console.WriteLine, variables, context.UpdateBuildNumber ?? true);
        }
        if (gitVersionOptions.Output.Contains(OutputType.File))
        {
            var retryOperation = new RetryAction<IOException>();
            retryOperation.Execute(() => this.fileSystem.WriteAllText(context.OutputFile, variables.ToJsonString()));
        }

        if (!gitVersionOptions.Output.Contains(OutputType.Json)) return;

        if (gitVersionOptions.ShowVariable is null && gitVersionOptions.Format is null)
        {
            this.console.WriteLine(variables.ToJsonString());
            return;
        }

        if (gitVersionOptions.ShowVariable is not null && gitVersionOptions.Format is not null)
        {
            throw new WarningException("Cannot specify both /showvariable and /format");
        }
        if (gitVersionOptions.ShowVariable is not null)
        {
            if (!variables.TryGetValue(gitVersionOptions.ShowVariable, out var part))
            {
                throw new WarningException($"'{gitVersionOptions.ShowVariable}' variable does not exist");
            }

            this.console.WriteLine(part);
            return;
        }
        if (gitVersionOptions.Format is not null)
        {
            var format = gitVersionOptions.Format;
            var formatted = format.FormatWith(variables, environment);
            this.console.WriteLine(formatted);
            return;
        }

        switch (gitVersionOptions.ShowVariable)
        {
            case null:
                this.console.WriteLine(variables.ToString());
                break;

            default:
                if (!variables.TryGetValue(gitVersionOptions.ShowVariable, out var part))
                {
                    throw new WarningException($"'{gitVersionOptions.ShowVariable}' variable does not exist");
                }

                this.console.WriteLine(part);
                break;
        }
    }

    public void Dispose()
    {
    }
}
