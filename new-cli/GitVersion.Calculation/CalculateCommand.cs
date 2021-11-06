using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Calculation;

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
        logger.LogInformation(
            $"Command : 'calculate', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
        return Task.FromResult(value);
    }
}