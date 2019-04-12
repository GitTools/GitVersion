namespace GitVersionTask
{
    using System;
    using System.IO;
    using GitVersion;
    using GitVersion.Helpers;
    using Microsoft.Build.Framework;

    public class GenerateGitVersionInformation : GitVersionTaskBase
    {
        [Required]
        public string ProjectFile { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }

        [Required]
        public string Language { get; set; }

        [Output]
        public string GitVersionInformationFilePath { get; set; }

        protected override void InnerExecute()
        {
            if (!ExecuteCore.TryGetVersion(SolutionDirectory, out var versionVariables, NoFetch, new Authentication()))
            {
                return;
            }

            var fileExtension = GetFileExtension();
            var fileName = $"GitVersionInformation.g.{fileExtension}";

            if (IntermediateOutputPath == null)
            {
                fileName = $"GitVersionInformation_{Path.GetFileNameWithoutExtension(ProjectFile)}_{Path.GetRandomFileName()}.g.{fileExtension}";
            }

            var workingDirectory = IntermediateOutputPath ?? TempFileTracker.TempPath;

            GitVersionInformationFilePath = Path.Combine(workingDirectory, fileName);

            var generator = new GitVersionInformationGenerator(fileName, workingDirectory, versionVariables, new FileSystem());
            generator.Generate();
        }

        string GetFileExtension()
        {
            switch (Language)
            {
                case "C#":
                    return "cs";

                case "F#":
                    return "fs";

                case "VB":
                    return "vb";

                default:
                    throw new Exception($"Unknown language detected: '{Language}'");
            }
        }
    }
}
