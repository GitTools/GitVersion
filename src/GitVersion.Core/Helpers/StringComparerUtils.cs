namespace GitVersion.Helpers;

public static class StringComparerUtils
{
    public static readonly StringComparer IgnoreCaseComparer = StringComparer.InvariantCultureIgnoreCase;
    public static readonly StringComparison OsDependentComparison = SysEnv.OSVersion.Platform == PlatformID.Unix ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    public static readonly StringComparer OsDependentComparer = SysEnv.OSVersion.Platform == PlatformID.Unix ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
}
