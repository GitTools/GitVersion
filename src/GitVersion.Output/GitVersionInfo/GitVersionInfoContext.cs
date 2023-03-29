namespace GitVersion.Output.GitVersionInfo;

internal readonly struct GitVersionInfoContext : IConverterContext
{
    public GitVersionInfoContext(string workingDirectory, string fileName, string fileExtension)
    {
        WorkingDirectory = workingDirectory;
        FileName = fileName;
        FileExtension = fileExtension;
    }

    public string WorkingDirectory { get; }
    public string FileName { get; }
    public string FileExtension { get; }
}
