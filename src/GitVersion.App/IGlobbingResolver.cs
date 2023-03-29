namespace GitVersion;

internal interface IGlobbingResolver
{
    public IEnumerable<string> Resolve(string workingDirectory, string pattern);
}
