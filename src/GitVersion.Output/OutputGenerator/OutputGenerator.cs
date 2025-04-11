using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion.Output.OutputGenerator;

internal interface IOutputGenerator : IVersionConverter<OutputContext>;

internal sealed class OutputGenerator(
    ICurrentBuildAgent buildAgent,
    IConsole console,
    IFileSystem fileSystem,
    IVersionVariableSerializer serializer,
    IEnvironment environment,
    IOptions<GitVersionOptions> options)
    : IOutputGenerator
{
    private readonly IConsole console = console.NotNull();
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly IVersionVariableSerializer serializer = serializer.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();
    private readonly ICurrentBuildAgent buildAgent = buildAgent.NotNull();

    public void Execute(GitVersionVariables variables, OutputContext context)
    {
        var gitVersionOptions = this.options.Value;

        if (gitVersionOptions.Output.Contains(OutputType.BuildServer))
        {
            this.buildAgent.WriteIntegration(this.console.WriteLine, variables, context.UpdateBuildNumber ?? true);
        }

        if (gitVersionOptions.Output.Contains(OutputType.DotEnv))
        {
            List<string> dotEnvEntries = [];
            foreach (var (key, value) in variables.OrderBy(x => x.Key))
            {
                var prefixedKey = "GitVersion_" + key;
                var environmentValue = "";
                if (!value.IsNullOrEmpty())
                {
                    environmentValue = value;
                }
                dotEnvEntries.Add($"{prefixedKey}='{environmentValue}'");
            }

            foreach (var dotEnvEntry in dotEnvEntries)
            {
                this.console.WriteLine(dotEnvEntry);
            }

            return;
        }

        var json = this.serializer.ToJson(variables);
        if (gitVersionOptions.Output.Contains(OutputType.File))
        {
            var retryOperation = new RetryAction<IOException>();
            retryOperation.Execute(() =>
            {
                if (context.OutputFile != null) this.fileSystem.File.WriteAllText(context.OutputFile, json);
            });
        }

        if (!gitVersionOptions.Output.Contains(OutputType.Json)) return;

        if (gitVersionOptions.ShowVariable is null && gitVersionOptions.Format is null)
        {
            this.console.WriteLine(json);
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
