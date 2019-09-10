using System.IO;
using System.Linq;
using GitVersion.OutputVariables;
using GitVersion.Common;
using Environment = System.Environment;

namespace GitVersion.Extensions.GitVersionInformationResources
{
    public class GitVersionInformationGenerator
    {
        string fileName;
        string directory;
        VersionVariables variables;
        IFileSystem fileSystem;

        TemplateManager templateManager;

        public GitVersionInformationGenerator(string fileName, string directory, VersionVariables variables, IFileSystem fileSystem)
        {
            this.fileName = fileName;
            this.directory = directory;
            this.variables = variables;
            this.fileSystem = fileSystem;

            templateManager = new TemplateManager(TemplateType.GitVersionInformationResources);
        }

        public void Generate()
        {
            var filePath = Path.Combine(directory, fileName);

            string originalFileContents = null;

            if (File.Exists(filePath))
            {
                originalFileContents = fileSystem.ReadAllText(filePath);
            }

            var fileExtension = Path.GetExtension(filePath);
            var template = templateManager.GetTemplateFor(fileExtension);
            var addFormat = templateManager.GetAddFormatFor(fileExtension);

            var members = string.Join(Environment.NewLine, variables.Select(v => string.Format("    " + addFormat, v.Key, v.Value)));

            var fileContents = string.Format(template, members);

            if (fileContents != originalFileContents)
            {
                fileSystem.WriteAllText(filePath, fileContents);
            }
        }
    }
}
