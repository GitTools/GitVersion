namespace Common.Addins.Cake.DotNetCoreFormat;

public sealed class DotNetCoreFormatToolRunner : DotNetCoreTool<DotNetCoreFormatSettings>
{
    public DotNetCoreFormatToolRunner(
        IFileSystem fileSystem,
        ICakeEnvironment environment,
        IProcessRunner processRunner,
        IToolLocator tools) : base(fileSystem, environment, processRunner, tools)
    {
    }

    public void Run(DotNetCoreFormatSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
        Run(settings, GetArguments(settings));
    }

    private static ProcessArgumentBuilder GetArguments(DotNetCoreFormatSettings settings)
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
