using System;

namespace GitVersion
{
    public static class StringComparerUtils
    {
        public static readonly StringComparer IgnoreCaseComparer = StringComparer.InvariantCultureIgnoreCase;
        public static readonly StringComparison IgnoreCaseComparison = StringComparison.InvariantCultureIgnoreCase;
        public static readonly StringComparison CaseSensitiveComparison = StringComparison.InvariantCulture;
        public static readonly StringComparison OSDependentComparison = Environment.OSVersion.Platform == PlatformID.Unix ? CaseSensitiveComparison : IgnoreCaseComparison;
    }
}
