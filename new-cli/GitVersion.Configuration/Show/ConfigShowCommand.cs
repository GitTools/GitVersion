using GitVersion.Infrastructure;

namespace GitVersion.Commands;

public record ConfigShowSettings : ConfigSettings;

[Command<ConfigCommand>("show", "Shows the effective configuration.")]
public class ConfigShowCommand : ICommand<ConfigShowSettings>
{
    private readonly ILogger logger;
    private readonly IService service;

    public ConfigShowCommand(ILogger logger, IService service)
    {
        this.logger = logger;
        this.service = service;
    }

    public Task<int> InvokeAsync(ConfigShowSettings settings)
    {
        var value = service.Call();
        logger.LogInformation($"Command : 'config show', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
