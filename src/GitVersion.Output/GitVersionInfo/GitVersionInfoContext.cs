namespace GitVersion.Output.GitVersionInfo;

internal readonly record struct GitVersionInfoContext(
    string WorkingDirectory,
    string FileName,
    string FileExtension,
    string? TargetNamespace = null)
    : IConverterContext
{
    public GitVersionInfoContext(string workingDirectory, string fileName, string fileExtension)
        : this(workingDirectory, fileName, fileExtension, null)
    {
    }
}
