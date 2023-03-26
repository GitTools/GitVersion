using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Output.WixUpdater;

public interface IWixVersionFileUpdater : IVersionConverter<WixVersionContext>
{
}
public sealed class WixVersionFileUpdater : IWixVersionFileUpdater
{
    private readonly IFileSystem fileSystem;
    private readonly ILog log;
    private string? wixVersionFile;
    public const string WixVersionFileName = "GitVersion_WixVersion.wxi";

    public WixVersionFileUpdater(ILog log, IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem.NotNull();
        this.log = log.NotNull();
    }

    public void Execute(GitVersionVariables variables, WixVersionContext context)
    {
        this.wixVersionFile = PathHelper.Combine(context.WorkingDirectory, WixVersionFileName);
        this.log.Info("Updating GitVersion_WixVersion.wxi");

        var doc = new XmlDocument();
        doc.LoadXml(GetWixFormatFromVersionVariables(variables));

        var xmlDecl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
        var root = doc.DocumentElement;
        doc.InsertBefore(xmlDecl, root);

        this.fileSystem.Delete(this.wixVersionFile);
        using var fs = this.fileSystem.OpenWrite(this.wixVersionFile);
        doc.Save(fs);
    }

    private static string GetWixFormatFromVersionVariables(GitVersionVariables variables)
    {
        var builder = new StringBuilder();
        builder.Append("<Include xmlns=\"http://schemas.microsoft.com/wix/2006/wi\">\n");
        var availableVariables = GitVersionVariables.AvailableVariables;
        foreach (var variable in availableVariables)
        {
            variables.TryGetValue(variable, out var value);
            builder.Append("\t<?define ").Append(variable).Append("=\"").Append(value).Append("\"?>\n");
        }
        builder.Append("</Include>\n");
        return builder.ToString();
    }

    public void Dispose() => this.log.Info($"Done writing {this.wixVersionFile}");
}
