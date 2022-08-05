using GitVersion.Infrastructure;

namespace GitVersion;

[Command("config", "Manages the GitVersion configuration file.")]
public class ConfigCommand : ICommand<ConfigSettings>
{
    private readonly ILogger logger;
    private readonly IService service;

    public ConfigCommand(ILogger logger, IService service)
    {
        this.logger = logger;
        this.service = service;
    }

    public Task<int> InvokeAsync(ConfigSettings settings)
    {
        var value = service.Call();
        logger.LogInformation($"Command : 'config', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
