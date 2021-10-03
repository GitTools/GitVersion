namespace GitVersion.Configuration.Init;

public class StepResult
{
    private StepResult() { }

    public static StepResult Ok() => new();

    public static StepResult InvalidResponseSelected() => new()
    {
        InvalidResponse = true
    };

    public static StepResult SaveAndExit() => new()
    {
        Save = true,
        Exit = true
    };

    public static StepResult ExitWithoutSaving() => new()
    {
        Save = false,
        Exit = true
    };

    public bool Exit { get; private set; }

    public bool Save { get; private set; }

    public bool InvalidResponse { get; private set; }
}
