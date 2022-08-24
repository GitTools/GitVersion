namespace GitVersion;

public class IgnoredState
{
    public static IgnoredState Ignored = new IgnoredState(true);
    public static IgnoredState Included = new IgnoredState(false);

    private IgnoredState(bool isIgnored)
    {
        IsIgnored = isIgnored;
    }

    public bool IsIgnored { get; }
    public bool IsIncluded => !IsIgnored;
}
