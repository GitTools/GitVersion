using GitVersion.Extensions;
using GitVersion.Git;
using Microsoft.Extensions.Logging;

namespace GitVersion.Commands;

public record CalculateSettings : GitVersionSettings;

[Command("calculate", "Calculates the version object from the git history.")]
public class CalculateCommand(ILogger logger, IService service, IGitRepository repository) : ICommand<CalculateSettings>
{
    private readonly ILogger _logger = logger.NotNull();
    private readonly IService _service = service.NotNull();
    private readonly IGitRepository _repository = repository.NotNull();

    public Task<int> InvokeAsync(CalculateSettings settings, CancellationToken cancellationToken = default)
    {
        var value = _service.Call();
        if (settings.WorkDir != null)
        {
            _repository.DiscoverRepository(settings.WorkDir.FullName);
            var branches = _repository.Branches.ToList();
            _logger.LogInformation("Command : 'calculate', LogFile : '{logFile}', WorkDir : '{workDir}' ",
                settings.LogFile, settings.WorkDir);
            _logger.LogInformation("Found {count} branches", branches.Count);
        }

        return Task.FromResult(value);
    }
}
