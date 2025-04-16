using System.IO.Abstractions;

namespace GitVersion.FileSystemGlobbing;

internal sealed class DirectoryInfoGlobbingWrapper
    : Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoBase
{
    private readonly IFileSystem fileSystem;
    private readonly IDirectoryInfo directoryInfo;
    private readonly bool isParentPath;

    public DirectoryInfoGlobbingWrapper(IFileSystem fileSystem, IDirectoryInfo directoryInfo)
        : this(fileSystem, directoryInfo, isParentPath: false)
    {
    }

    private DirectoryInfoGlobbingWrapper(
        IFileSystem fileSystem,
        IDirectoryInfo directoryInfo,
        bool isParentPath
    )
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        this.directoryInfo = directoryInfo ?? throw new ArgumentNullException(nameof(directoryInfo));
        this.isParentPath = isParentPath;
    }

    public override string Name => this.isParentPath ? ".." : this.directoryInfo.Name;

    public override string FullName => this.directoryInfo.FullName;

    public override Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoBase? ParentDirectory =>
        this.directoryInfo.Parent is null
            ? null
            : new DirectoryInfoGlobbingWrapper(this.fileSystem, this.directoryInfo.Parent);

    public override IEnumerable<Microsoft.Extensions.FileSystemGlobbing.Abstractions.FileSystemInfoBase> EnumerateFileSystemInfos()
    {
        if (this.directoryInfo.Exists)
        {
            IEnumerable<IFileSystemInfo> fileSystemInfos;
            try
            {
                fileSystemInfos = this.directoryInfo.EnumerateFileSystemInfos(
                    "*",
                    SearchOption.TopDirectoryOnly
                );
            }
            catch (DirectoryNotFoundException)
            {
                yield break;
            }

            foreach (var fileSystemInfo in fileSystemInfos)
            {
                yield return fileSystemInfo switch
                {
                    IDirectoryInfo info => new DirectoryInfoGlobbingWrapper(this.fileSystem, info),
                    IFileInfo info => new FileInfoGlobbingWrapper(this.fileSystem, info),
                    _ => new FileSystemInfoGlobbingWrapper(this.fileSystem, fileSystemInfo),
                };
            }
        }
    }

    public override Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoBase? GetDirectory(string path)
    {
        var parentPath = string.Equals(path, "..", StringComparison.Ordinal);

        if (parentPath)
        {
            return new DirectoryInfoGlobbingWrapper(
                this.fileSystem,
                this.fileSystem.DirectoryInfo.New(this.fileSystem.Path.Combine(this.directoryInfo.FullName, path)),
                parentPath
            );
        }

        var dirs = this.directoryInfo.GetDirectories(path);

        return dirs switch
        {
            { Length: 1 } => new DirectoryInfoGlobbingWrapper(this.fileSystem, dirs[0], parentPath),
            { Length: 0 } => null,
            _ => throw new InvalidOperationException($"More than one sub directories are found under {this.directoryInfo.FullName} with name {path}."),
        };
    }

    public override Microsoft.Extensions.FileSystemGlobbing.Abstractions.FileInfoBase GetFile(string path)
        => new FileInfoGlobbingWrapper(
            this.fileSystem,
            this.fileSystem.FileInfo.New(this.fileSystem.Path.Combine(FullName, path))
        );
}
