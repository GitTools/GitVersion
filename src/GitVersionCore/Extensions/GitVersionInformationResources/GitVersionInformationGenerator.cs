using System.IO;
using System.Linq;
using GitVersion.OutputVariables;

namespace GitVersion.Extensions.GitVersionInformationResources
{
    public class GitVersionInformationGenerator
    {
        private readonly string fileName;
        private readonly string directory;
        private readonly VersionVariables variables;
        private readonly IFileSystem fileSystem;

        private readonly TemplateManager templateManager;

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

            var members = string.Join(System.Environment.NewLine, variables.Select(v => string.Format("    " + addFormat, v.Key, v.Value)));

            var fileContents = string.Format(template, members);

            if (fileContents != originalFileContents)
            {
                fileSystem.WriteAllText(filePath, fileContents);
            }
        }
    }
}
