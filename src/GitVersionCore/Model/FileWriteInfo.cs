namespace GitVersion
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
}
