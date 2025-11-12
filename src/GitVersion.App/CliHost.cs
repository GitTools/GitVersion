using System.IO.Abstractions;
using GitVersion;
using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Output;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace GitVersion;

internal static class CliHost
{
    internal static HostApplicationBuilder CreateCliHostBuilder(string[] args)
    {
        // Parse arguments early to configure logging
        var tempServices = new ServiceCollection();
        tempServices.AddSingleton<IArgumentParser, ArgumentParser>();
        var tempProvider = tempServices.BuildServiceProvider();
        var argumentParser = tempProvider.GetRequiredService<IArgumentParser>();
        var arguments = argumentParser.ParseArguments(args);
        var gitVersionOptions = arguments.ToOptions();

        var builder = Host.CreateApplicationBuilder(args);

        // Configure Serilog based on parsed arguments
        ConfigureSerilog(builder, gitVersionOptions);

        builder.Services.AddModule(new GitVersionCoreModule());
        builder.Services.AddModule(new GitVersionLibGit2SharpModule());
        builder.Services.AddModule(new GitVersionBuildAgentsModule());
        builder.Services.AddModule(new GitVersionConfigurationModule());
        builder.Services.AddModule(new GitVersionOutputModule());
        builder.Services.AddModule(new GitVersionAppModule());

        builder.Services.AddSingleton(sp => Options.Create(gitVersionOptions));

        builder.Services.AddSingleton<GitVersionApp>();

        return builder;
    }

    private static void ConfigureSerilog(HostApplicationBuilder builder, GitVersionOptions options)
    {
        var loggerConfiguration = LoggingModule.CreateLoggerConfiguration(
            options.Verbosity,
            options.Output.Contains(OutputType.BuildServer) || options.LogFilePath == "console",
            options.LogFilePath != null && options.LogFilePath != "console" ? options.LogFilePath : null,
            new FileSystem()
        );

        Serilog.Log.Logger = loggerConfiguration.CreateLogger();

        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Serilog.Log.Logger, dispose: true);
        });
    }
}
