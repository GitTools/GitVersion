namespace GitVersion;

/// <summary>
/// Storage for parse results
/// </summary>
internal class ParseResultStorage
{
    private Arguments? result;

    public void SetResult(Arguments arguments) => this.result = arguments;
    public Arguments? GetResult() => this.result;
}