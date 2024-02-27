using GitVersion.Extensions;
using GitVersion.Infrastructure;

namespace GitVersion.Commands;

public record CalculateSettings : GitVersionSettings;

[Command("calculate", "Calculates the version object from the git history.")]
public class CalculateCommand(ILogger logger, IService service, IGitRepository repository) : ICommand<CalculateSettings>
{
    private readonly ILogger logger = logger.NotNull();
    private readonly IService service = service.NotNull();
    private readonly IGitRepository repository = repository.NotNull();

    public Task<int> InvokeAsync(CalculateSettings settings, CancellationToken cancellationToken = default)
    {
        var value = service.Call();
        if (settings.WorkDir != null)
        {
            this.repository.DiscoverRepository(settings.WorkDir.FullName);
            var branches = this.repository.Branches.ToList();
            this.logger.LogInformation("Command : 'calculate', LogFile : '{logFile}', WorkDir : '{workDir}' ",
                settings.LogFile, settings.WorkDir);
            this.logger.LogInformation("Found {count} branches", branches.Count);
        }

        return Task.FromResult(value);
    }
}
