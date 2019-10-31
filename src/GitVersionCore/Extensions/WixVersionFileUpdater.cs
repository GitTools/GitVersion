using System;
using System.IO;
using System.Text;
using System.Xml;
using GitVersion.OutputVariables;
using GitVersion.Logging;

namespace GitVersion.Extensions
{
    public class WixVersionFileUpdater : IDisposable
    {
        private readonly VersionVariables variables;
        private readonly IFileSystem fileSystem;
        private readonly ILog log;
        public string WixVersionFile { get; }
        public const string WixVersionFileName = "GitVersion_WixVersion.wxi";

        public WixVersionFileUpdater(string workingDirectory, VersionVariables variables, IFileSystem fileSystem, ILog log)
        {
            this.variables = variables;
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.WixVersionFile = Path.Combine(workingDirectory, WixVersionFileName);
        }

        public void Update()
        {
            log.Info("Updating GitVersion_WixVersion.wxi");

            var doc = new XmlDocument();
            doc.LoadXml(GetWixFormatFromVersionVariables());

            var xmlDecl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            var root = doc.DocumentElement;
            doc.InsertBefore(xmlDecl, root);

            using var fs = fileSystem.OpenWrite(WixVersionFile);
            doc.Save(fs);
        }

        private string GetWixFormatFromVersionVariables()
        {
            var builder = new StringBuilder();
            builder.Append("<Include xmlns=\"http://schemas.microsoft.com/wix/2006/wi\">\n");
            var availableVariables = VersionVariables.AvailableVariables;
            foreach (var variable in availableVariables)
            {
                variables.TryGetValue(variable, out var value);
                builder.Append($"\t<?define {variable}=\"{value}\"?>\n");
            }
            builder.Append("</Include>\n");
            return builder.ToString();
        }

        public void Dispose()
        {
            log.Info($"Done writing {WixVersionFile}");
        }
    }
}
