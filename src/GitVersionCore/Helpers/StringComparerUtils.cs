using System;

namespace GitVersion.Helpers
{
    public static class StringComparerUtils
    {
        public static readonly StringComparer IgnoreCaseComparer = StringComparer.InvariantCultureIgnoreCase;
        public static readonly StringComparison OsDependentComparison = System.Environment.OSVersion.Platform == PlatformID.Unix ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
    }
}
