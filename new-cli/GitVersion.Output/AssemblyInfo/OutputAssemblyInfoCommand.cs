using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

[Command<OutputCommand>("assemblyinfo", "Outputs version to assembly")]
public class OutputAssemblyInfoCommand(ILogger logger, IService service) : ICommand<OutputAssemblyInfoSettings>
{
    private readonly ILogger _logger = logger.NotNull();
    private readonly IService _service = service.NotNull();

    public Task<int> InvokeAsync(OutputAssemblyInfoSettings settings, CancellationToken cancellationToken = default)
    {
        var value = _service.Call();
        var versionInfo = settings.VersionInfo.Value;
        _logger.LogInformation($"Command : 'output assemblyinfo', LogFile : '{settings.LogFile}', WorkDir : '{settings.OutputDir}', InputFile: '{settings.InputFile}', AssemblyInfo: '{settings.AssemblyinfoFile}' ");
        _logger.LogInformation($"Version info: {versionInfo}");
        return Task.FromResult(value);
    }
}
