using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

public record ConfigInitSettings : ConfigSettings;

[Command<ConfigCommand>("init", "Inits the configuration for current repository.")]
public class ConfigInitCommand(ILogger<ConfigInitCommand> logger, IService service) : ICommand<ConfigInitSettings>
{
    private readonly ILogger logger = logger.NotNull();
    private readonly IService service = service.NotNull();

    public Task<int> InvokeAsync(ConfigInitSettings settings, CancellationToken cancellationToken = default)
    {
        var value = this.service.Call();
        this.logger.LogInformation($"Command : 'config init', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
