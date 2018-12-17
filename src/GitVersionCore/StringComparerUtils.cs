using System;

namespace GitVersion
{
    public static class StringComparerUtils
    {
        public static readonly System.StringComparer IngoreCaseComparer = StringComparer.InvariantCultureIgnoreCase;
        public static readonly StringComparison IngoreCaseComparison = StringComparison.InvariantCultureIgnoreCase;
        public static readonly StringComparison CaseSensitiveComparison = StringComparison.InvariantCulture;
    }
}
