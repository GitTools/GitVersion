namespace GitVersionTask
{
    using GitVersion;
    using GitVersion.Helpers;
    using System;
    using System.IO;

    public static class GitVersionTaskBase
    {
        internal static TOutput ExecuteGitVersionTask<TInput, TOutput>(
            TInput input,
            Func<TInput, TaskLogger, TOutput> execute
            )
            where TInput : AbstractInput
            where TOutput : class, new()
        {
            if (!input.ValidateInput())
            {
                throw new Exception("Invalid input.");
            }

            var logger = new TaskLogger();
            Logger.SetLoggers(logger.LogInfo, logger.LogInfo, logger.LogWarning, s => logger.LogError(s));


            TOutput output = null;
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

        private static string GetFileExtension(this String language)
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
                    throw new Exception($"Unknown language detected: '{language}'");
            }
        }

        public static FileWriteInfo GetWorkingDirectoryAndFileNameAndExtension(
            this String intermediateOutputPath,
            String language,
            String projectFile,
            Func<String, String, String> fileNameWithIntermediatePath,
            Func<String, String, String> fileNameNoIntermediatePath
            )
        {
            var fileExtension = language.GetFileExtension();
            String workingDirectory, fileName;
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

        public abstract class AbstractInput
        {
            public String SolutionDirectory { get; set; }

            public Boolean NoFetch { get; set; }

            public virtual Boolean ValidateInput()
            {
                return !String.IsNullOrEmpty(this.SolutionDirectory);
            }
        }

        public abstract class InputWithCommonAdditionalProperties : AbstractInput
        {
            public String ProjectFile { get; set; }

            public String IntermediateOutputPath { get; set; }

            public String Language { get; set; }

            public override Boolean ValidateInput()
            {
                return base.ValidateInput()
                    && !String.IsNullOrEmpty(this.ProjectFile)
                    && !String.IsNullOrEmpty(this.IntermediateOutputPath)
                    && !String.IsNullOrEmpty(this.Language);
            }
        }
    }

    public sealed class FileWriteInfo
    {
        public FileWriteInfo(
            String workingDirectory,
            String fileName,
            String fileExtension
            )
        {
            this.WorkingDirectory = workingDirectory;
            this.FileName = fileName;
            this.FileExtension = fileExtension;
        }

        public String WorkingDirectory { get; }
        public String FileName { get; }
        public String FileExtension { get; }
    }
}
