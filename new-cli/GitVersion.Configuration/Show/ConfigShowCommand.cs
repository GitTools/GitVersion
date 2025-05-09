using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

public record ConfigShowSettings : ConfigSettings;

[Command<ConfigCommand>("show", "Shows the effective configuration.")]
public class ConfigShowCommand(ILogger logger, IService service) : ICommand<ConfigShowSettings>
{
    private readonly ILogger _logger = logger.NotNull();
    private readonly IService _service = service.NotNull();

    public Task<int> InvokeAsync(ConfigShowSettings settings, CancellationToken cancellationToken = default)
    {
        var value = _service.Call();
        _logger.LogInformation($"Command : 'config show', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
