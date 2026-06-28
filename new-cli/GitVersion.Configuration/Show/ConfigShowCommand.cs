using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

public record ConfigShowSettings : ConfigSettings;

[Command<ConfigCommand>("show", "Shows the effective configuration.")]
public class ConfigShowCommand(ILogger<ConfigShowCommand> logger, IService service) : ICommand<ConfigShowSettings>
{
    private readonly ILogger logger = logger.NotNull();
    private readonly IService service = service.NotNull();

    public Task<int> InvokeAsync(ConfigShowSettings settings, CancellationToken cancellationToken = default)
    {
        var value = this.service.Call();
        this.logger.LogInformation("Command : 'config show', LogFile : '{LogFile}', WorkDir : '{WorkDir}' ", settings.LogFile, settings.WorkDir);
        return Task.FromResult(value);
    }
}
