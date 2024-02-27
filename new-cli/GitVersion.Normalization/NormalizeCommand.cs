using GitVersion.Infrastructure;

namespace GitVersion.Commands;

public record NormalizeSettings : GitVersionSettings;

[Command("normalize", "Normalizes the git repository for GitVersion calculations.")]
public class NormalizeCommand : ICommand<NormalizeSettings>
{
    private readonly ILogger logger;
    private readonly IService service;

    public NormalizeCommand(ILogger logger, IService service)
    {
        this.logger = logger;
        this.service = service;
    }

    public Task<int> InvokeAsync(NormalizeSettings settings, CancellationToken cancellationToken = default)
    {
        var value = service.Call();
        logger.LogInformation($"Command : 'normalize', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
