using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

public record ConfigSettings : GitVersionSettings;

[Command("config", "Manages the GitVersion configuration file.")]
public class ConfigCommand(ILogger<ConfigCommand> logger, IService service) : ICommand<ConfigSettings>
{
    private readonly ILogger _logger = logger.NotNull();
    private readonly IService _service = service.NotNull();

    public Task<int> InvokeAsync(ConfigSettings settings, CancellationToken cancellationToken = default)
    {
        var value = _service.Call();
        _logger.LogInformation($"Command : 'config', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
