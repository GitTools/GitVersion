using GitVersion.Logging;

namespace GitVersion;

public class HelpWriter : IHelpWriter
{
    private readonly IVersionWriter versionWriter;
    private readonly IConsole console;

    public HelpWriter(IVersionWriter versionWriter, IConsole console)
    {
        this.versionWriter = versionWriter ?? throw new ArgumentNullException(nameof(versionWriter));
        this.console = console ?? throw new ArgumentNullException(nameof(console));
    }

    public void Write() => WriteTo(this.console.WriteLine);

    public void WriteTo(Action<string> writeAction)
    {
        var version = string.Empty;
        var assembly = Assembly.GetExecutingAssembly();
        this.versionWriter.WriteTo(assembly, v => version = v);

        var args = ArgumentList();
        var nl = System.Environment.NewLine;
        var message = "GitVersion " + version + nl + nl + args;

        writeAction(message);
    }

    private string ArgumentList()
    {
        using var argumentsMarkdownStream = GetType().Assembly.GetManifestResourceStream("GitVersion.arguments.md");
        using var sr = new StreamReader(argumentsMarkdownStream);
        var argsMarkdown = sr.ReadToEnd();
        var codeBlockStart = argsMarkdown.IndexOf("```") + 3;
        var codeBlockEnd = argsMarkdown.LastIndexOf("```") - codeBlockStart;
        return argsMarkdown.Substring(codeBlockStart, codeBlockEnd).Trim();
    }
}
