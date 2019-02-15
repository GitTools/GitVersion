namespace GitVersionTask
{
    using System;
    using System.IO;
    using GitVersion;
    using GitVersion.Helpers;
    using Microsoft.Build.Framework;

    public class GenerateGitVersionInformation : GitVersionTaskBase
    {
        TaskLogger logger;

        public GenerateGitVersionInformation()
        {
            logger = new TaskLogger(this);
            Logger.SetLoggers(this.LogDebug, this.LogInfo, this.LogWarning, s => this.LogError(s));
        }

        [Required]
        public string SolutionDirectory { get; set; }

        [Required]
        public string ProjectFile { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }

        [Required]
        public string Language { get; set; }

        [Output]
        public string GitVersionInformationFilePath { get; set; }

        public bool NoFetch { get; set; }

        public override bool Execute()
        {
            try
            {
                InnerExecute();
                return true;
            }
            catch (WarningException errorException)
            {
                logger.LogWarning(errorException.Message);
                return true;
            }
            catch (Exception exception)
            {
                logger.LogError("Error occurred: " + exception);
                return false;
            }
            finally
            {
                Logger.Reset();
            }
        }

        void InnerExecute()
        {
            VersionVariables versionVariables;
            if (!ExecuteCore.TryGetVersion(SolutionDirectory, out versionVariables, NoFetch, new Authentication()))
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
