using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.GitVersionInfo;

public interface IGitVersionInfoGenerator : IVersionConverter<GitVersionInfoContext>
{
}

public sealed class GitVersionInfoGenerator : IGitVersionInfoGenerator
{
    private readonly IFileSystem fileSystem;
    private readonly TemplateManager templateManager;

    public GitVersionInfoGenerator(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        this.templateManager = new TemplateManager(TemplateType.GitVersionInfo);
    }

    public void Execute(VersionVariables variables, GitVersionInfoContext context)
    {
        var fileName = context.FileName;
        var directory = context.WorkingDirectory;
        var filePath = Path.Combine(directory, fileName);

        string? originalFileContents = null;

        if (File.Exists(filePath))
        {
            originalFileContents = this.fileSystem.ReadAllText(filePath);
        }

        var fileExtension = Path.GetExtension(filePath);
        var template = this.templateManager.GetTemplateFor(fileExtension);
        var addFormat = this.templateManager.GetAddFormatFor(fileExtension);
        var indentation = GetIndentation(fileExtension);

        var members = string.Join(System.Environment.NewLine, variables.Select(v => string.Format(indentation + addFormat, v.Key, v.Value)));

        var fileContents = string.Format(template, members);

        if (fileContents != originalFileContents)
        {
            this.fileSystem.WriteAllText(filePath, fileContents);
        }
    }

    public void Dispose()
    {
    }

    // Because The VB-generated class is included in a namespace declaration,
    // the properties must be offsetted by 2 tabs.
    // Whereas in the C# and F# cases, 1 tab is enough.
    private static string GetIndentation(string fileExtension)
    {
        var tabs = fileExtension.ToLowerInvariant().EndsWith("vb") ? 2 : 1;
        return new string(' ', tabs * 4);
    }
}
