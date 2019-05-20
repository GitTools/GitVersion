namespace GitVersion
{
    using Helpers;

    using System;
    using System.Text;
    using System.Xml;

    public class WixVersionFileUpdater : IDisposable
    {
        string workingDirectory;
        VersionVariables variables;
        IFileSystem fileSystem;
        private const string WIX_VERSION_FILE = "GitVersion_WixVersion.wxi ";

        public WixVersionFileUpdater(string workingDirectory, VersionVariables variables, IFileSystem fileSystem)
        {
            this.workingDirectory = workingDirectory;
            this.variables = variables;
            this.fileSystem = fileSystem;
        }
                
        public static string GetWixVersionFileName()
        {
            return WIX_VERSION_FILE;
        }

        public void Update()
        {
            Logger.WriteInfo("Updating GitVersion_WixVersion.wxi");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetWixFormatFromVersionVariables());

            XmlDeclaration xmlDecl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDecl, root);

            using (var fs = fileSystem.OpenWrite(WIX_VERSION_FILE))
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
                string value;
                variables.TryGetValue(variable, out value);
                builder.Append(string.Format("\t<?define {0}=\"{1}\"?>\n", variable, value));
            }
            builder.Append("</Include>\n");
            return builder.ToString();
        }

        public void Dispose()
        {
            Logger.WriteInfo(string.Format("Done writing {0}", WIX_VERSION_FILE));
        }
    }
}
