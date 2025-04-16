namespace GitVersion.FileSystemGlobbing;

internal interface IGlobbingResolver
{
    IEnumerable<string> Resolve(string workingDirectory, string pattern);
}
