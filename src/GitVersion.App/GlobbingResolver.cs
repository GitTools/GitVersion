using Microsoft.Extensions.FileSystemGlobbing;

namespace GitVersion;

internal class GlobbingResolver : IGlobbingResolver
{
    private readonly Matcher matcher = new(StringComparison.OrdinalIgnoreCase);

    public IEnumerable<string> Resolve(string workingDirectory, string pattern)
    {
        this.matcher.AddInclude(pattern);
        return this.matcher.GetResultsInFullPath(workingDirectory);
    }
}
