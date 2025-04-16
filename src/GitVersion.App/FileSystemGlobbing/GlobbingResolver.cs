using System.IO.Abstractions;
using GitVersion.Extensions;
using Microsoft.Extensions.FileSystemGlobbing;

namespace GitVersion.FileSystemGlobbing;

internal class GlobbingResolver(IFileSystem fileSystem) : IGlobbingResolver
{
    private readonly IFileSystem fileSystem = fileSystem.NotNull();

    public IEnumerable<string> Resolve(string workingDirectory, string pattern)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(pattern);
        return matcher.GetResultsInFullPath(this.fileSystem, workingDirectory);
    }
}
