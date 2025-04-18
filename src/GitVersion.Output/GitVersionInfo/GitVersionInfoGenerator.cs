using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.OutputVariables;

namespace GitVersion.Output.GitVersionInfo;

internal interface IGitVersionInfoGenerator : IVersionConverter<GitVersionInfoContext>;

internal sealed class GitVersionInfoGenerator(IFileSystem fileSystem) : IGitVersionInfoGenerator
{
    private const string targetNamespaceSentinelValue = "<unset>";
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly TemplateManager templateManager = new(TemplateType.GitVersionInfo);

    public void Execute(GitVersionVariables variables, GitVersionInfoContext context)
    {
        var fileName = context.FileName;
        var directory = context.WorkingDirectory;
        var filePath = FileSystemHelper.Path.Combine(directory, fileName);

        string? originalFileContents = null;

        if (this.fileSystem.File.Exists(filePath))
        {
            originalFileContents = this.fileSystem.File.ReadAllText(filePath);
        }

        var fileExtension = FileSystemHelper.Path.GetExtension(filePath);
        ArgumentNullException.ThrowIfNull(fileExtension);

        var template = this.templateManager.GetTemplateFor(fileExtension);
        var addFormat = this.templateManager.GetAddFormatFor(fileExtension);
        var targetNamespace = getTargetNamespace(fileExtension);

        if (string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(addFormat) || targetNamespace == targetNamespaceSentinelValue)
            return;

        var indentation = GetIndentation(fileExtension);
        string? closeBracket = null;
        string? openBracket = null;
        var indent = "";

        if (!string.IsNullOrWhiteSpace(targetNamespace) && fileExtension == ".cs")
        {
            indent = "    ";
            closeBracket = FileSystemHelper.Path.NewLine + "}";
            openBracket = FileSystemHelper.Path.NewLine + "{";
            indentation += "    ";
        }

        var lines = variables.OrderBy(x => x.Key).Select(v => string.Format(indentation + addFormat, v.Key, v.Value));
        var members = string.Join(FileSystemHelper.Path.NewLine, lines);

        var fileContents = string.Format(template, members, targetNamespace, openBracket, closeBracket, indent);

        if (fileContents != originalFileContents)
        {
            this.fileSystem.File.WriteAllText(filePath, fileContents);
        }

        return;

        string getTargetNamespace(string? extension) => extension switch
        {
            ".vb" => context.TargetNamespace ?? "Global",
            ".cs" => context.TargetNamespace != null ? $"{FileSystemHelper.Path.NewLine}namespace {context.TargetNamespace}" : "",
            ".fs" => context.TargetNamespace ?? "global",
            _ => targetNamespaceSentinelValue,
        };
    }

    public void Dispose()
    {
    }

    // Because The VB-generated class is included in a namespace declaration,
    // the properties must be offset by 2 tabs.
    // Whereas in the C# and F# cases, 1 tab is enough.
    private static string GetIndentation(string fileExtension)
    {
        var tabs = fileExtension.EndsWith("vb", StringComparison.InvariantCultureIgnoreCase) ? 2 : 1;
        return new string(' ', tabs * 4);
    }
}
