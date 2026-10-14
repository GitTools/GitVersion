using Microsoft.Build.Framework;

namespace GitVersion.MsBuild.Tests.Mocks;

internal class MockTaskItem : ITaskItem
{
    public string ItemSpec { get; set; } = null!;

    public int MetadataCount { get; set; }

    public ICollection MetadataNames { get; set; } = null!;

    public IDictionary CloneCustomMetadata() => throw new NotImplementedException(); // NOSONAR: ITaskItem requires an instance method.

    public void CopyMetadataTo(ITaskItem destinationItem) => throw new NotImplementedException(); // NOSONAR: ITaskItem requires an instance method.

    public string GetMetadata(string metadataName) => throw new NotImplementedException(); // NOSONAR: ITaskItem requires an instance method.

    public void RemoveMetadata(string metadataName) => throw new NotImplementedException(); // NOSONAR: ITaskItem requires an instance method.

    public void SetMetadata(string metadataName, string metadataValue) => throw new NotImplementedException(); // NOSONAR: ITaskItem requires an instance method.
}
