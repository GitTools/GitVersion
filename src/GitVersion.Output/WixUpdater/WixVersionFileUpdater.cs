using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.OutputVariables;

namespace GitVersion.Output.WixUpdater;

internal interface IWixVersionFileUpdater : IVersionConverter<WixVersionContext>;
internal sealed class WixVersionFileUpdater(ILogger<WixVersionFileUpdater> logger, IFileSystem fileSystem) : IWixVersionFileUpdater
{
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly ILogger<WixVersionFileUpdater> logger = logger.NotNull();
    private string? wixVersionFile;
    public const string WixVersionFileName = "GitVersion_WixVersion.wxi";

    public void Execute(GitVersionVariables variables, WixVersionContext context)
    {
        this.wixVersionFile = FileSystemHelper.Path.Combine(context.WorkingDirectory, WixVersionFileName);
        this.logger.LogInformation("Updating GitVersion_WixVersion.wxi");

        var doc = new XmlDocument();
        doc.LoadXml(GetWixFormatFromVersionVariables(variables));

        var xmlDecl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
        var root = doc.DocumentElement;
        doc.InsertBefore(xmlDecl, root);

        if (this.fileSystem.File.Exists(this.wixVersionFile))
        {
            this.fileSystem.File.Delete(this.wixVersionFile);
        }

        if (!this.fileSystem.Directory.Exists(context.WorkingDirectory))
        {
            this.fileSystem.Directory.CreateDirectory(context.WorkingDirectory);
        }
        using var fs = this.fileSystem.File.OpenWrite(this.wixVersionFile);
        doc.Save(fs);
    }

    private static string GetWixFormatFromVersionVariables(GitVersionVariables variables)
    {
        var builder = new StringBuilder();
        builder.Append("<Include xmlns=\"http://schemas.microsoft.com/wix/2006/wi\">\n");
        foreach (var (key, value) in variables.OrderBy(x => x.Key))
        {
            builder.Append("\t<?define ").Append(key).Append("=\"").Append(value).Append("\"?>\n");
        }
        builder.Append("</Include>\n");
        return builder.ToString();
    }

    public void Dispose()
    {
    }
}
