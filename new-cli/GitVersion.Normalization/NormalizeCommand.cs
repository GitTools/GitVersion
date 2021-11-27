using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Normalization;

[Command("normalize", "Normalizes the git repository for GitVersion calculations.")]
public class NormalizeCommand : Command<NormalizeSettings>
{
    private readonly ILogger logger;
    private readonly IService service;

    public NormalizeCommand(ILogger logger, IService service)
    {
        this.logger = logger;
        this.service = service;
    }

    public override Task<int> InvokeAsync(NormalizeSettings settings)
    {
        var value = service.Call();
        logger.LogInformation($"Command : 'normalize', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}