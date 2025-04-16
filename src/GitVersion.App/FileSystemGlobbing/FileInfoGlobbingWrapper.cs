using System.IO.Abstractions;
using GitVersion.Extensions;

namespace GitVersion.FileSystemGlobbing;

/// <summary>
/// Initialize a new instance
/// </summary>
/// <param name="fileSystem">The filesystem</param>
/// <param name="fileInfo">The file</param>
internal sealed class FileInfoGlobbingWrapper(IFileSystem fileSystem, IFileInfo fileInfo)
        : Microsoft.Extensions.FileSystemGlobbing.Abstractions.FileInfoBase
{
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly IFileInfo fileInfo = fileInfo.NotNull();

    /// <inheritdoc />
    public override string Name => this.fileInfo.Name;

    /// <inheritdoc />
    public override string FullName => this.fileInfo.FullName;

    /// <inheritdoc />
    public override Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoBase? ParentDirectory =>
        this.fileInfo.Directory is null
            ? null
            : new DirectoryInfoGlobbingWrapper(this.fileSystem, this.fileInfo.Directory);
}
