namespace GitVersion.Output.GitVersionInfo;

internal readonly struct GitVersionInfoContext : IConverterContext
{
    public GitVersionInfoContext(string workingDirectory, string fileName, string fileExtension) : this(workingDirectory, fileName, fileExtension, null)
    {
    }

    public GitVersionInfoContext(string workingDirectory, string fileName, string fileExtension, string? targetNamespace = null)
    {
        WorkingDirectory = workingDirectory;
        FileName = fileName;
        FileExtension = fileExtension;
        TargetNamespace = targetNamespace;
    }

    public string WorkingDirectory { get; }
    public string FileName { get; }
    public string FileExtension { get; }
    public string? TargetNamespace { get; }
}
