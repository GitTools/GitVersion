using GitVersion.Git;
using GitVersion.Infrastructure;

namespace GitVersion;

[Command("calculate", "Calculates the version object from the git history.")]
public class CalculateCommand : Command<CalculateSettings>
{
    private readonly ILogger logger;
    private readonly IService service;
    private readonly IGitRepository repository;

    public CalculateCommand(ILogger logger, IService service, IGitRepository repository)
    {
        this.logger = logger;
        this.service = service;
        this.repository = repository;
    }

    public override Task<int> InvokeAsync(CalculateSettings settings)
    {
        var value = service.Call();
        this.repository.Discover(settings.WorkDir.FullName);
        var branches = this.repository.Branches.ToList();
        logger.LogInformation("Command : 'calculate', LogFile : '{logFile}', WorkDir : '{workDir}' ", settings.LogFile, settings.WorkDir);
        logger.LogInformation("Found {count} branches", branches.Count);
        return Task.FromResult(value);
    }
}
