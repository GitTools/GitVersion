using GitVersion.Extensions;
using GitVersion.Infrastructure;

namespace GitVersion.Commands;

public record ConfigSettings : GitVersionSettings;

[Command("config", "Manages the GitVersion configuration file.")]
public class ConfigCommand(ILogger logger, IService service) : ICommand<ConfigSettings>
{
    private readonly ILogger logger = logger.NotNull();
    private readonly IService service = service.NotNull();

    public Task<int> InvokeAsync(ConfigSettings settings, CancellationToken cancellationToken = default)
    {
        var value = service.Call();
        logger.LogInformation($"Command : 'config', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
