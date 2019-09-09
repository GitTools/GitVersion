using System;
using System.IO;
using System.Text;
using System.Xml;
using GitVersion.Helpers;
using GitVersion.OutputVariables;
using GitVersion.Common;

namespace GitVersion.Extensions
{
    public class WixVersionFileUpdater : IDisposable
    {
        VersionVariables variables;
        IFileSystem fileSystem;
        public string WixVersionFile { get; }
        public const string WIX_VERSION_FILE = "GitVersion_WixVersion.wxi";

        public WixVersionFileUpdater(string workingDirectory, VersionVariables variables, IFileSystem fileSystem)
        {
            this.variables = variables;
            this.fileSystem = fileSystem;
            this.WixVersionFile = Path.Combine(workingDirectory, WIX_VERSION_FILE);
        }

        public void Update()
        {
            Logger.WriteInfo("Updating GitVersion_WixVersion.wxi");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetWixFormatFromVersionVariables());

            XmlDeclaration xmlDecl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDecl, root);

            using (var fs = fileSystem.OpenWrite(WixVersionFile))
            {
                doc.Save(fs);
            }
        }

        private string GetWixFormatFromVersionVariables()
        {
            StringBuilder builder = new StringBuilder();
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
            Logger.WriteInfo($"Done writing {WixVersionFile}");
        }
    }
}
