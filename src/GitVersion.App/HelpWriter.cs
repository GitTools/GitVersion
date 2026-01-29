using GitVersion.Extensions;
using GitVersion.Helpers;

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

        var args = ArgumentList();
        var message = $"GitVersion {version}{FileSystemHelper.Path.NewLine}{FileSystemHelper.Path.NewLine}{args}";

        writeAction(message);
    }

    private string ArgumentList()
    {
        using var argumentsMarkdownStream = GetType().Assembly.GetManifestResourceStream("GitVersion.arguments.md");
        argumentsMarkdownStream.NotNull();
        using var sr = new StreamReader(argumentsMarkdownStream);
        var argsMarkdown = sr.ReadToEnd();
        var codeBlockStart = argsMarkdown.IndexOf("```", StringComparison.Ordinal) + 3;
        var codeBlockEnd = argsMarkdown.LastIndexOf("```", StringComparison.Ordinal) - codeBlockStart;
        return argsMarkdown.Substring(codeBlockStart, codeBlockEnd).Trim();
    }
}
