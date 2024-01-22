namespace GitVersion;

public sealed class FileWriteInfo(string workingDirectory, string fileName, string fileExtension)
{
    public string WorkingDirectory { get; } = workingDirectory;
    public string FileName { get; } = fileName;
    public string FileExtension { get; } = fileExtension;
}
