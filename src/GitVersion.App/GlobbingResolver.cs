using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace GitVersion;

public class GlobbingResolver : IGlobbingResolver
{
    private readonly Matcher matcher = new(StringComparison.OrdinalIgnoreCase);

    public IEnumerable<string> Resolve(string workingDirectory, string pattern)
    {
        this.matcher.AddInclude(pattern);
        return this.matcher.Execute(GetDirectoryInfoWrapper(workingDirectory)).Files.Select(file => file.Path);
    }

    protected virtual DirectoryInfoBase GetDirectoryInfoWrapper(string workingDirectory) => new DirectoryInfoWrapper(new DirectoryInfo(workingDirectory));
}
