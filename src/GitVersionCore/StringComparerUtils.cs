using System;

namespace GitVersion
{
    public static class StringComparerUtils
    {
        public static readonly StringComparer IgnoreCaseComparer = StringComparer.InvariantCultureIgnoreCase;
        public static readonly StringComparison IgnoreCaseComparison = StringComparison.InvariantCultureIgnoreCase;
        public static readonly StringComparison CaseSensitiveComparison = StringComparison.InvariantCulture;
    }
}
