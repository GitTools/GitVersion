namespace GitVersion.Output.GitVersionInfo;

internal readonly struct GitVersionInfoContext(string workingDirectory, string fileName, string fileExtension, string? targetNamespace = null)
    : IConverterContext
{
    public GitVersionInfoContext(string workingDirectory, string fileName, string fileExtension) : this(workingDirectory, fileName, fileExtension, null)
    {
    }

    public string WorkingDirectory { get; } = workingDirectory;
    public string FileName { get; } = fileName;
    public string FileExtension { get; } = fileExtension;
    public string? TargetNamespace { get; } = targetNamespace;
}
