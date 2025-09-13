using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Output;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GitVersion;

internal static class CliHost
{
    internal static HostApplicationBuilder CreateCliHostBuilder(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddModule(new GitVersionCoreModule());
        builder.Services.AddModule(new GitVersionLibGit2SharpModule());
        builder.Services.AddModule(new GitVersionBuildAgentsModule());
        builder.Services.AddModule(new GitVersionConfigurationModule());
        builder.Services.AddModule(new GitVersionOutputModule());
        builder.Services.AddModule(new GitVersionAppModule());

        builder.Services.AddSingleton(sp =>
        {
            var arguments = sp.GetRequiredService<IArgumentParser>().ParseArguments(args);
            var gitVersionOptions = arguments.ToOptions();
            return Options.Create(gitVersionOptions);
        });

        builder.Services.AddSingleton<GitVersionApp>();

        return builder;
    }
}
