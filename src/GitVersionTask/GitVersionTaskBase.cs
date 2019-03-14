namespace GitVersionTask
{
    using GitVersion;
    using GitVersion.Helpers;
    using System;
    using System.IO;

    public static class GitVersionTaskBase
    {
        public static ExecuteCore CreateExecuteCore()
            => new ExecuteCore( new FileSystem() );

        private static string GetFileExtension( this String language )
        {
            switch ( language )
            {
                case "C#":
                    return "cs";

                case "F#":
                    return "fs";

                case "VB":
                    return "vb";

                default:
                    throw new Exception( $"Unknown language detected: '{language}'" );
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
            if ( intermediateOutputPath == null )
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
