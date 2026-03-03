using GitVersion.Extensions;

namespace GitVersion;

internal class HelpWriter(IVersionWriter versionWriter, IConsole console) : IHelpWriter
{
    private readonly IVersionWriter versionWriter = versionWriter.NotNull();
    private readonly IConsole console = console.NotNull();

    public void Write() => WriteTo(this.console.WriteLine);

    public void WriteTo(Action<string> writeAction)
    {
        var version = string.Empty;
        var assembly = Assembly.GetExecutingAssembly();
        this.versionWriter.WriteTo(assembly, v => version = v);

        var args = LegacyArgumentList();
        var message = $"""
                       GitVersion {version}

                       {args}
                       """;

        writeAction(message);
    }

    private string LegacyArgumentList()
    {
        using var argumentsMarkdownStream = GetType().Assembly.GetManifestResourceStream("GitVersion.arguments.md");
        argumentsMarkdownStream.NotNull();
        using var sr = new StreamReader(argumentsMarkdownStream);
        var argsMarkdown = sr.ReadToEnd();
        var codeBlockStart = argsMarkdown.IndexOf("```bash", StringComparison.Ordinal) + 7;
        var codeBlockEnd = argsMarkdown.LastIndexOf("```", StringComparison.Ordinal) - codeBlockStart;
        return argsMarkdown.Substring(codeBlockStart, codeBlockEnd).Trim();
    }
}
