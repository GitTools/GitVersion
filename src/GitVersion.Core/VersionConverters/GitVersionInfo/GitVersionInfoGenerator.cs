using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.OutputVariables;
using Polly.CircuitBreaker;

namespace GitVersion.VersionConverters.GitVersionInfo;

public interface IGitVersionInfoGenerator : IVersionConverter<GitVersionInfoContext>
{
}

public sealed class GitVersionInfoGenerator : IGitVersionInfoGenerator
{
    private const string targetNamespaceSentinelValue = "<unset>";
    private readonly IFileSystem fileSystem;
    private readonly TemplateManager templateManager;

    public GitVersionInfoGenerator(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem.NotNull();
        templateManager = new TemplateManager(TemplateType.GitVersionInfo);
    }

    public void Execute(VersionVariables variables, GitVersionInfoContext context)
    {
        var fileName = context.FileName;
        var directory = context.WorkingDirectory;
        var filePath = PathHelper.Combine(directory, fileName);

        string? originalFileContents = null;


        if (File.Exists(filePath))
        {
            originalFileContents = fileSystem.ReadAllText(filePath);
        }

        var fileExtension = Path.GetExtension(filePath);
        var template = templateManager.GetTemplateFor(fileExtension);
        var addFormat = templateManager.GetAddFormatFor(fileExtension);
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
            closeBracket = System.Environment.NewLine + "}";
            openBracket = System.Environment.NewLine + "{";
            indentation += "    ";
        }
        var members = string.Join(System.Environment.NewLine, variables.Select(v => string.Format(indentation + addFormat, v.Key, v.Value)));


        var fileContents = string.Format(template, members, targetNamespace, openBracket, closeBracket, indent);

        if (fileContents != originalFileContents)
        {
            fileSystem.WriteAllText(filePath, fileContents);
        }

        string getTargetNamespace(string fileExtension) => fileExtension switch
        {
            ".vb" => context.TargetNamespace ?? "Global",
            ".cs" => context.TargetNamespace != null ? $"{System.Environment.NewLine}namespace {context.TargetNamespace};" : "",
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
        var tabs = fileExtension.ToLowerInvariant().EndsWith("vb") ? 2 : 1;
        return new string(' ', tabs * 4);
    }
}
