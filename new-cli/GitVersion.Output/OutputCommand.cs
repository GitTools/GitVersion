using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output;

public class OutputCommand : Command<OutputSettings>
{
    private readonly ILogger logger;
    private readonly IService service;

    public OutputCommand(ILogger logger, IService service)
    {
        this.logger = logger;
        this.service = service;
    }

    public override Task<int> InvokeAsync(OutputSettings settings)
    {
        var value = service.Call();
        logger.LogInformation(
            $"Command : 'output', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}