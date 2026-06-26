using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

[Command<OutputCommand>("assemblyinfo", "Outputs version to assembly")]
public class OutputAssemblyInfoCommand(ILogger<OutputAssemblyInfoCommand> logger, IService service) : ICommand<OutputAssemblyInfoSettings>
{
    private readonly ILogger logger = logger.NotNull();
    private readonly IService service = service.NotNull();

    public Task<int> InvokeAsync(OutputAssemblyInfoSettings settings, CancellationToken cancellationToken = default)
    {
        var value = this.service.Call();
        var versionInfo = settings.VersionInfo.Value;
        this.logger.LogInformation($"Command : 'output assemblyinfo', LogFile : '{settings.LogFile}', WorkDir : '{settings.OutputDir}', InputFile: '{settings.InputFile}', AssemblyInfo: '{settings.AssemblyinfoFile}' ");
        this.logger.LogInformation($"Version info: {versionInfo}");
        return Task.FromResult(value);
    }
}
