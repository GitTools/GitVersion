using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

public record NormalizeSettings : GitVersionSettings;

[Command("normalize", "Normalizes the git repository for GitVersion calculations.")]
public class NormalizeCommand(ILogger logger, IService service) : ICommand<NormalizeSettings>
{
    private readonly ILogger logger = logger.NotNull();
    private readonly IService service = service.NotNull();

    public Task<int> InvokeAsync(NormalizeSettings settings, CancellationToken cancellationToken = default)
    {
        var value = service.Call();
        logger.LogInformation($"Command : 'normalize', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
