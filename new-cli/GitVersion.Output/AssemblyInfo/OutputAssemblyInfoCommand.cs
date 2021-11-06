using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output.AssemblyInfo;

public class OutputAssemblyInfoCommand : Command<OutputAssemblyInfoSettings>
{
    private readonly ILogger logger;
    private readonly IService service;

    public OutputAssemblyInfoCommand(ILogger logger, IService service)
    {
        this.logger = logger;
        this.service = service;
    }

    public override Task<int> InvokeAsync(OutputAssemblyInfoSettings settings)
    {
        var value = service.Call();
        var versionInfo = settings.VersionInfo.Value;
        logger.LogInformation(
            $"Command : 'output assemblyinfo', LogFile : '{settings.LogFile}', WorkDir : '{settings.OutputDir}', InputFile: '{settings.InputFile}', AssemblyInfo: '{settings.AssemblyinfoFile}' ");
        logger.LogInformation($"Version info: {versionInfo}");
        return Task.FromResult(value);
    }
}