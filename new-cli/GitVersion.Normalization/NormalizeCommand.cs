using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

public record NormalizeSettings : GitVersionSettings; // NOSONAR: Distinct command settings type inheriting shared options.

[Command("normalize", "Normalizes the git repository for GitVersion calculations.")]
public class NormalizeCommand(ILogger<NormalizeCommand> logger, IService service) : ICommand<NormalizeSettings>
{
    private readonly ILogger logger = logger.NotNull();
    private readonly IService service = service.NotNull();

    public Task<int> InvokeAsync(NormalizeSettings settings, CancellationToken cancellationToken = default)
    {
        var value = this.service.Call();
        this.logger.LogInformation("Command : 'normalize', LogFile : '{LogFile}', WorkDir : '{WorkDir}' ", settings.LogFile, settings.WorkDir);
        return Task.FromResult(value);
    }
}
