using GitVersion.Commands.Test.Settings;

namespace GitVersion.Commands.Test;

[Command("test", "Test command.")]
public class TestCommand : ICommand<TestCommandSettings>
{
    public Task<int> InvokeAsync(TestCommandSettings settings, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Input file: {0}", settings.InputFile);
        return Task.FromResult(0);
    }
}
