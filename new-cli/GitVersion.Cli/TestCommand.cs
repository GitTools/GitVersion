namespace GitVersion;

[Command("test", "Test command.")]
public class TestCommand: ICommand<TestCommandSettings>
{
    public Task<int> InvokeAsync(TestCommandSettings settings, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Input file: {0}", settings.InputFile);
        return Task.FromResult(0);
    }
}

public record TestCommandSettings : GitVersionSettings
{
    [Option("--input-file", "The input version file")]
    public required string InputFile { get; init; }
}
