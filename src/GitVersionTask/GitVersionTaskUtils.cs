namespace GitVersionTask
{
    using System.IO;

    public static class GitVersionTaskUtils
    {
        public static FileWriteInfo GetFileWriteInfo(this string intermediateOutputPath, string language, string projectFile, string outputFileName)
        {
            var fileExtension = FileHelper.GetFileExtension(language);
            string workingDirectory, fileName;

            if (intermediateOutputPath == null)
            {
                fileName = $"{outputFileName}.g.{fileExtension}";
                workingDirectory = FileHelper.TempPath;
            }
            else
            {
                fileName = $"{outputFileName}_{Path.GetFileNameWithoutExtension(projectFile)}_{Path.GetRandomFileName()}.g.{fileExtension}";
                workingDirectory = intermediateOutputPath;
            }
            return new FileWriteInfo(workingDirectory, fileName, fileExtension);
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
