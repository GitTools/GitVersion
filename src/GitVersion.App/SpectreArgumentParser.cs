using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.FileSystemGlobbing;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Spectre.Console.Cli;

namespace GitVersion;

/// <summary>
/// Argument parser that uses Spectre.Console.Cli for enhanced command line processing
/// with POSIX compliant syntax
/// </summary>
internal class SpectreArgumentParser : IArgumentParser
{
    private readonly IEnvironment environment;
    private readonly IFileSystem fileSystem;
    private readonly ICurrentBuildAgent buildAgent;
    private readonly IConsole console;
    private readonly IGlobbingResolver globbingResolver;

    public SpectreArgumentParser(
        IEnvironment environment,
        IFileSystem fileSystem,
        ICurrentBuildAgent buildAgent,
        IConsole console,
        IGlobbingResolver globbingResolver)
    {
        this.environment = environment.NotNull();
        this.fileSystem = fileSystem.NotNull();
        this.buildAgent = buildAgent.NotNull();
        this.console = console.NotNull();
        this.globbingResolver = globbingResolver.NotNull();
    }

    public Arguments ParseArguments(string commandLineArguments)
    {
        var arguments = QuotedStringHelpers.SplitUnquoted(commandLineArguments, ' ');
        return ParseArguments(arguments);
    }

    public Arguments ParseArguments(string[] commandLineArguments)
    {
        // Handle empty arguments
        if (commandLineArguments.Length == 0)
        {
            return CreateDefaultArguments();
        }

        // Handle help requests  
        var firstArg = commandLineArguments[0];
        if (firstArg.IsHelp())
        {
            return new Arguments { IsHelp = true };
        }

        // Handle version requests
        if (firstArg.IsSwitch("version"))
        {
            return new Arguments { IsVersion = true };
        }

        // Use Spectre.Console.Cli to parse arguments
        var app = new CommandApp<GitVersionCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("gitversion");
            config.PropagateExceptions();
        });

        var resultStorage = new ParseResultStorage();

        try
        {
            // Parse the arguments
            var interceptor = new ArgumentInterceptor(resultStorage, this.environment, this.fileSystem, this.buildAgent, this.console, this.globbingResolver);
#pragma warning disable CS0618 // Type or member is obsolete
            app.Configure(config => config.Settings.Interceptor = interceptor);
#pragma warning restore CS0618 // Type or member is obsolete

            var parseResult = app.Run(commandLineArguments);

            var result = resultStorage.GetResult();
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception)
        {
            // If parsing fails, return default arguments
            return CreateDefaultArguments();
        }

        return CreateDefaultArguments();
    }

    private Arguments CreateDefaultArguments()
    {
        var args = new Arguments
        {
            TargetPath = SysEnv.CurrentDirectory
        };
        args.Output.Add(OutputType.Json);
        AddAuthentication(args);
        args.NoFetch = this.buildAgent.PreventFetch();
        return args;
    }

    private void AddAuthentication(Arguments arguments)
    {
        var username = this.environment.GetEnvironmentVariable("GITVERSION_REMOTE_USERNAME");
        if (!username.IsNullOrWhiteSpace())
        {
            arguments.Authentication.Username = username;
        }

        var password = this.environment.GetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD");
        if (!password.IsNullOrWhiteSpace())
        {
            arguments.Authentication.Password = password;
        }
    }
}
