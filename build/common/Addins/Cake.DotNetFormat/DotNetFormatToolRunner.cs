namespace Common.Addins.Cake.DotNetFormat;

public sealed class DotNetFormatToolRunner : DotNetTool<DotNetFormatSettings>
{
    public DotNetFormatToolRunner(
        IFileSystem fileSystem,
        ICakeEnvironment environment,
        IProcessRunner processRunner,
        IToolLocator tools) : base(fileSystem, environment, processRunner, tools)
    {
    }

    public void Run(DotNetFormatSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
        Run(settings, GetArguments(settings));
    }

    private static ProcessArgumentBuilder GetArguments(DotNetFormatSettings settings)
    {
        ProcessArgumentBuilder arguments = new();

        arguments.Append("format");
        switch (settings.Fix)
        {
            case DotNetFormatFix.Whitespace:
                arguments.Append("whitespace");
                break;
            case DotNetFormatFix.Style:
                arguments.Append("style");
                break;
            case DotNetFormatFix.Analyzers:
                arguments.Append("analyzers");
                break;
        }

        arguments.Append(settings.Workspace != null ? settings.Workspace.ToString() : settings.WorkingDirectory.ToString());

        if (settings.Diagnostics != null)
        {
            arguments.AppendSwitch("--diagnostics", " ", string.Join(" ", settings.Diagnostics));
        }
        if (settings.Include != null)
        {
            arguments.AppendSwitch("--include", " ", string.Join(" ", settings.Include));
        }
        if (settings.Exclude != null)
        {
            arguments.AppendSwitch("--exclude", " ", string.Join(" ", settings.Exclude));
        }
        if (settings.VerifyNoChanges)
        {
            arguments.Append("--verify-no-changes");
        }
        return arguments;
    }
}
