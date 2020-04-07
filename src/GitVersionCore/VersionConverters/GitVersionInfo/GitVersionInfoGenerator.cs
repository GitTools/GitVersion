using System;
using System.IO;
using System.Linq;
using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.GitVersionInfo
{
    public interface IGitVersionInfoGenerator : IVersionConverter<GitVersionInfoContext>
    {
    }

    public class GitVersionInfoGenerator : IGitVersionInfoGenerator
    {
        private readonly IFileSystem fileSystem;
        private readonly TemplateManager templateManager;

        public GitVersionInfoGenerator(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            templateManager = new TemplateManager(TemplateType.GitVersionInfo);
        }

        public void Execute(VersionVariables variables, GitVersionInfoContext context)
        {
            var fileName = context.FileName;
            var directory = context.WorkingDirectory;
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

        public void Dispose()
        {
        }
    }
}
