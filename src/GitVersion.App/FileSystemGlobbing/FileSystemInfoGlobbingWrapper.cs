using System.IO.Abstractions;
using GitVersion.Extensions;

namespace GitVersion.FileSystemGlobbing;

internal sealed class FileSystemInfoGlobbingWrapper(IFileSystem fileSystem, IFileSystemInfo fileSystemInfo) : Microsoft.Extensions.FileSystemGlobbing.Abstractions.FileSystemInfoBase
{
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly IFileSystemInfo fileSystemInfo = fileSystemInfo.NotNull();

    public override string Name => this.fileSystemInfo.Name;

    public override string FullName => this.fileSystemInfo.FullName;

    public override Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoBase ParentDirectory =>
        new DirectoryInfoGlobbingWrapper(
            this.fileSystem,
            this.fileSystem.DirectoryInfo.New(this.fileSystemInfo.FullName)
        );
}
