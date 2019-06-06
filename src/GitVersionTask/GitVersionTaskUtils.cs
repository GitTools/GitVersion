namespace GitVersionTask
{
    using GitVersion;
    using GitVersion.Helpers;
    using System;

    public static class GitVersionTaskUtils
    {
        public static bool GetVersionVariables(InputBase input, out VersionVariables versionVariables)
            => new ExecuteCore(new FileSystem()).TryGetVersion(input.SolutionDirectory, out versionVariables, input.NoFetch, new Authentication());

        public static FileWriteInfo GetFileWriteInfo(
            this string intermediateOutputPath,
            string language,
            string projectFile,
            Func<string, string, string> fileNameWithIntermediatePath,
            Func<string, string, string> fileNameNoIntermediatePath
            )
        {
            var fileExtension = FileHelper.GetFileExtension(language);
            string workingDirectory, fileName;
            if (intermediateOutputPath == null)
            {
                fileName = fileNameWithIntermediatePath(projectFile, fileExtension);
                workingDirectory = FileHelper.TempPath;
            }
            else
            {
                workingDirectory = intermediateOutputPath;
                fileName = fileNameNoIntermediatePath(projectFile, fileExtension);
            }
            return new FileWriteInfo(workingDirectory, fileName, fileExtension);
        }
    }

    public abstract class InputBase
    {
        public string SolutionDirectory { get; set; }

        public bool NoFetch { get; set; }

        public void ValidateInputOrThrowException()
        {
            if (!ValidateInput())
            {
                throw new InputValidationException($"Invalid input for {GetType()}.");
            }
        }

        protected virtual bool ValidateInput()
        {
            return !string.IsNullOrEmpty(SolutionDirectory);
        }
    }

    public abstract class InputWithCommonAdditionalProperties : InputBase
    {
        public string ProjectFile { get; set; }

        public string IntermediateOutputPath { get; set; }

        public string Language { get; set; }

        protected override bool ValidateInput()
        {
            return base.ValidateInput()
                && !string.IsNullOrEmpty(ProjectFile)
                && !string.IsNullOrEmpty(IntermediateOutputPath)
                && !string.IsNullOrEmpty(Language);
        }
    }

    public sealed class InputValidationException : Exception
    {
        public InputValidationException(string msg, Exception inner = null)
            : base(msg, inner)
        {

        }
    }

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
}
