using GitVersion.Extensions;
using GitVersion.Infrastructure;

namespace GitVersion.Commands;

public record ConfigInitSettings : ConfigSettings;

[Command<ConfigCommand>("init", "Inits the configuration for current repository.")]
public class ConfigInitCommand(ILogger logger, IService service) : ICommand<ConfigInitSettings>
{
    private readonly ILogger logger = logger.NotNull();
    private readonly IService service = service.NotNull();

    public Task<int> InvokeAsync(ConfigInitSettings settings, CancellationToken cancellationToken = default)
    {
        var value = service.Call();
        logger.LogInformation($"Command : 'config init', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
