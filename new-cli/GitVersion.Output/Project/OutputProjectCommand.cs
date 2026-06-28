using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

[Command<OutputCommand>("project", "Outputs version to project")]
public class OutputProjectCommand(ILogger<OutputProjectCommand> logger, IService service) : ICommand<OutputProjectSettings>
{
    private readonly ILogger logger = logger.NotNull();
    private readonly IService service = service.NotNull();

    public Task<int> InvokeAsync(OutputProjectSettings settings, CancellationToken cancellationToken = default)
    {
        var value = this.service.Call();
        this.logger.LogInformation("Command : 'output project', LogFile : '{LogFile}', WorkDir : '{OutputDir}', InputFile: '{InputFile}', Project: '{ProjectFile}' ", settings.LogFile, settings.OutputDir, settings.InputFile, settings.ProjectFile);
        return Task.FromResult(value);
    }
}
