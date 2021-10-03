using GitVersion.VersionConverters.AssemblyInfo;
using GitVersion.VersionConverters.GitVersionInfo;
using GitVersion.VersionConverters.OutputGenerator;
using GitVersion.VersionConverters.WixUpdater;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.VersionConverters;

public class VersionConvertersModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IOutputGenerator, OutputGenerator.OutputGenerator>();
        services.AddSingleton<IGitVersionInfoGenerator, GitVersionInfoGenerator>();
        services.AddSingleton<IWixVersionFileUpdater, WixVersionFileUpdater>();
        services.AddSingleton<IAssemblyInfoFileUpdater, AssemblyInfoFileUpdater>();
        services.AddSingleton<IProjectFileUpdater, ProjectFileUpdater>();
    }
}
