using GitVersion.Infrastructure;

namespace GitVersion.Commands;

public record ConfigInitSettings : ConfigSettings;

[Command<ConfigCommand>("init", "Inits the configuration for current repository.")]
public class ConfigInitCommand : ICommand<ConfigInitSettings>
{
    private readonly ILogger logger;
    private readonly IService service;

    public ConfigInitCommand(ILogger logger, IService service)
    {
        this.logger = logger;
        this.service = service;
    }

    public Task<int> InvokeAsync(ConfigInitSettings settings)
    {
        var value = service.Call();
        logger.LogInformation($"Command : 'config init', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
