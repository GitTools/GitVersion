using GitVersion.Command;
using GitVersion.Infrastructure;
namespace GitVersion.Calculation;

[Command("calculate", "Calculates the version object from the git history.")]
public class CalculateCommand : Command<CalculateSettings>
{
    private readonly ILogger logger;
    private readonly IService service;

    public CalculateCommand(ILogger logger, IService service)
    {
        this.logger = logger;
        this.service = service;
    }

    public override Task<int> InvokeAsync(CalculateSettings settings)
    {
        var value = service.Call();
        // logger.LogInformation($"Command : 'calculate', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        logger.LogInformation("Command : 'calculate', LogFile : '{logFile}', WorkDir : '{workDir}' ", settings.LogFile, settings.WorkDir);
        return Task.FromResult(value);
    }
}