using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

[Command<OutputCommand>("project", "Outputs version to project")]
public class OutputProjectCommand(ILogger logger, IService service) : ICommand<OutputProjectSettings>
{
    private readonly ILogger _logger = logger.NotNull();
    private readonly IService _service = service.NotNull();

    public Task<int> InvokeAsync(OutputProjectSettings settings, CancellationToken cancellationToken = default)
    {
        var value = _service.Call();
        _logger.LogInformation($"Command : 'output project', LogFile : '{settings.LogFile}', WorkDir : '{settings.OutputDir}', InputFile: '{settings.InputFile}', Project: '{settings.ProjectFile}' ");
        return Task.FromResult(value);
    }
}
