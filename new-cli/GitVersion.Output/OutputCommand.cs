using GitVersion.Infrastructure;

namespace GitVersion.Commands;

[Command("output", "Outputs the version object.")]
public class OutputCommand : ICommand<OutputSettings>
{
    private readonly ILogger logger;
    private readonly IService service;

    public OutputCommand(ILogger logger, IService service)
    {
        this.logger = logger;
        this.service = service;
    }

    public Task<int> InvokeAsync(OutputSettings settings, CancellationToken cancellationToken = default)
    {
        var value = service.Call();
        logger.LogInformation($"Command : 'output', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}
