using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

[Command<OutputCommand>("wix", "Outputs version to wix file")]
public class OutputWixCommand(ILogger<OutputWixCommand> logger, IService service) : ICommand<OutputWixSettings>
{
    private readonly ILogger logger = logger.NotNull();
    private readonly IService service = service.NotNull();

    public Task<int> InvokeAsync(OutputWixSettings settings, CancellationToken cancellationToken = default)
    {
        var value = this.service.Call();
        this.logger.LogInformation($"Command : 'output wix', LogFile : '{settings.LogFile}', WorkDir : '{settings.OutputDir}', InputFile: '{settings.InputFile}', WixFile: '{settings.WixFile}' ");
        return Task.FromResult(value);
    }
}
