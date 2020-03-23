using System;
using System.IO;
using System.Linq;
using GitVersion.OutputVariables;

namespace GitVersion.Extensions.GitVersionInformationResources
{
    public sealed class FileWriteInfo
    {
        public FileWriteInfo(string workingDirectory, string fileName, string fileExtension)
        {
            WorkingDirectory = workingDirectory;
            FileName = fileName;
            FileExtension = fileExtension;
        }

        public string WorkingDirectory { get; }
        public string FileName { get; }
        public string FileExtension { get; }
    }


    public class GitVersionInformationGenerator
    {
        private readonly IFileSystem fileSystem;
        private readonly TemplateManager templateManager;

        public GitVersionInformationGenerator(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            templateManager = new TemplateManager(TemplateType.GitVersionInformationResources);
        }

        public void Generate(VersionVariables variables, FileWriteInfo writeInfo)
        {
            var fileName = writeInfo.FileName;
            var directory = writeInfo.WorkingDirectory;
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
