namespace GitVersionTask
{
    using GitVersion;
    using GitVersion.Helpers;
    using System;

    public static class GitVersionTaskCommonFunctionality
    {
        internal static TOutput ExecuteGitVersionTask<TInput, TOutput>(TInput input, Func<TInput, TaskLogger, TOutput> execute)
            where TInput : InputBase
            where TOutput : class, new()
        {

            input.ValidateInputOrThrowException();

            var logger = new TaskLogger();
            Logger.SetLoggers(logger.LogInfo, logger.LogInfo, logger.LogWarning, s => logger.LogError(s));


            TOutput output;
            try
            {
                output = execute(input, logger);
            }
            catch (WarningException errorException)
            {
                logger.LogWarning(errorException.Message);
                output = new TOutput();
            }
            catch (Exception exception)
            {
                logger.LogError("Error occurred: " + exception);
                throw;
            }
            finally
            {
                Logger.Reset();
            }

            return output;
        }


        public static ExecuteCore CreateExecuteCore()
            => new ExecuteCore(new FileSystem());

        private static string GetFileExtension(this string language)
        {
            switch (language)
            {
                case "C#":
                    return "cs";

                case "F#":
                    return "fs";

                case "VB":
                    return "vb";

                default:
                    throw new ArgumentException($"Unknown language detected: '{language}'");
            }
        }

        public static FileWriteInfo GetFileWriteInfo(
            this string intermediateOutputPath,
            string language,
            string projectFile,
            Func<string, string, string> fileNameWithIntermediatePath,
            Func<string, string, string> fileNameNoIntermediatePath
            )
        {
            var fileExtension = language.GetFileExtension();
            string workingDirectory, fileName;
            if (intermediateOutputPath == null)
            {
                fileName = fileNameWithIntermediatePath(projectFile, fileExtension);
                workingDirectory = TempFileTracker.TempPath;
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
