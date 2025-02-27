namespace GitVersion;

internal interface IGlobbingResolver
{
    IEnumerable<string> Resolve(string workingDirectory, string pattern);
}
