using GitVersion.Infrastructure;

namespace GitVersion.Commands;

[Command<OutputCommand>("assemblyinfo", "Outputs version to assembly")]
public class OutputAssemblyInfoCommand : ICommand<OutputAssemblyInfoSettings>
{
    private readonly ILogger logger;
    private readonly IService service;

    public OutputAssemblyInfoCommand(ILogger logger, IService service)
    {
        this.logger = logger;
        this.service = service;
    }

    public Task<int> InvokeAsync(OutputAssemblyInfoSettings settings)
    {
        var value = service.Call();
        var versionInfo = settings.VersionInfo.Value;
        logger.LogInformation($"Command : 'output assemblyinfo', LogFile : '{settings.LogFile}', WorkDir : '{settings.OutputDir}', InputFile: '{settings.InputFile}', AssemblyInfo: '{settings.AssemblyinfoFile}' ");
        logger.LogInformation($"Version info: {versionInfo}");
        return Task.FromResult(value);
    }
}
