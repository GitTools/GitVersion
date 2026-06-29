using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

[Command("output", "Outputs the version object.")]
public class OutputCommand(ILogger<OutputCommand> logger, IService service) : ICommand<OutputSettings>
{
    private readonly ILogger logger = logger.NotNull();
    private readonly IService service = service.NotNull();

    public Task<int> InvokeAsync(OutputSettings settings, CancellationToken cancellationToken = default)
    {
        var value = this.service.Call();
        this.logger.LogInformation("Command : 'output', LogFile : '{LogFile}', WorkDir : '{WorkDir}' ", settings.LogFile, settings.WorkDir);
        return Task.FromResult(value);
    }
}
