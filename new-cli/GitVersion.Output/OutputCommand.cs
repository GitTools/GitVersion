using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

[Command("output", "Outputs the version object.")]
public class OutputCommand(ILogger<OutputCommand> logger, IService service) : ICommand<OutputSettings>
{
    private readonly ILogger _logger = logger.NotNull();
    private readonly IService _service = service.NotNull();

    public Task<int> InvokeAsync(OutputSettings settings, CancellationToken cancellationToken = default)
    {
        var value = _service.Call();
        _logger.LogInformation($"Command : 'output', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
