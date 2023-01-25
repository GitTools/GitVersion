using GitVersion.Infrastructure;

namespace GitVersion.Commands;

[Command<OutputCommand>("project", "Outputs version to project")]
public class OutputProjectCommand : ICommand<OutputProjectSettings>
{
    private readonly ILogger logger;
    private readonly IService service;

    public OutputProjectCommand(ILogger logger, IService service)
    {
        this.logger = logger;
        this.service = service;
    }

    public Task<int> InvokeAsync(OutputProjectSettings settings)
    {
        var value = service.Call();
        logger.LogInformation($"Command : 'output project', LogFile : '{settings.LogFile}', WorkDir : '{settings.OutputDir}', InputFile: '{settings.InputFile}', Project: '{settings.ProjectFile}' ");
        return Task.FromResult(value);
    }
}
