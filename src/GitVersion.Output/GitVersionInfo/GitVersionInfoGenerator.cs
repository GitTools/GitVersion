using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.OutputVariables;

namespace GitVersion.Output.GitVersionInfo;

internal interface IGitVersionInfoGenerator : IVersionConverter<GitVersionInfoContext>
{
}

internal sealed class GitVersionInfoGenerator : IGitVersionInfoGenerator
{
    private const string targetNamespaceSentinelValue = "<unset>";
    private readonly IFileSystem fileSystem;
    private readonly TemplateManager templateManager;

    public GitVersionInfoGenerator(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem.NotNull();
        this.templateManager = new TemplateManager(TemplateType.GitVersionInfo);
    }

    public void Execute(GitVersionVariables variables, GitVersionInfoContext context)
    {
        var fileName = context.FileName;
        var directory = context.WorkingDirectory;
        var filePath = PathHelper.Combine(directory, fileName);

        string? originalFileContents = null;

        if (File.Exists(filePath))
        {
            originalFileContents = this.fileSystem.ReadAllText(filePath);
        }

        var fileExtension = Path.GetExtension(filePath);
        var template = this.templateManager.GetTemplateFor(fileExtension);
        var addFormat = this.templateManager.GetAddFormatFor(fileExtension);
        var targetNamespace = getTargetNamespace(fileExtension);

        if (string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(addFormat) || targetNamespace == targetNamespaceSentinelValue)
            return;

        var indentation = GetIndentation(fileExtension);
        string? closeBracket = null;
        string? openBracket = null;
        string indent = "";

        if (!string.IsNullOrWhiteSpace(targetNamespace) && fileExtension == ".cs")
        {
            indent = "    ";
            closeBracket = PathHelper.NewLine + "}";
            openBracket = PathHelper.NewLine + "{";
            indentation += "    ";
        }

        var lines = variables.OrderBy(x => x.Key).Select(v => string.Format(indentation + addFormat, v.Key, v.Value));
        var members = string.Join(PathHelper.NewLine, lines);

        var fileContents = string.Format(template, members, targetNamespace, openBracket, closeBracket, indent);

        if (fileContents != originalFileContents)
        {
            this.fileSystem.WriteAllText(filePath, fileContents);
        }

        string getTargetNamespace(string fileExtension) => fileExtension switch
        {
            ".vb" => context.TargetNamespace ?? "Global",
            ".cs" => context.TargetNamespace != null ? $"{PathHelper.NewLine}namespace {context.TargetNamespace};" : "",
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
