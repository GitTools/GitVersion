using GitVersion.Extensions;
using GitVersion.Infrastructure;

namespace GitVersion.Commands;

[Command("output", "Outputs the version object.")]
public class OutputCommand(ILogger logger, IService service) : ICommand<OutputSettings>
{
    private readonly ILogger logger = logger.NotNull();
    private readonly IService service = service.NotNull();

    public Task<int> InvokeAsync(OutputSettings settings, CancellationToken cancellationToken = default)
    {
        var value = service.Call();
        logger.LogInformation($"Command : 'output', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
