using System;

namespace GitVersion
{
    public static class StringComparerUtils
    {
#if NETDESKTOP
        public static readonly System.StringComparer IngoreCaseComparer = StringComparer.InvariantCultureIgnoreCase;
        public static readonly StringComparison IngoreCaseComparison = StringComparison.InvariantCultureIgnoreCase;
        public static readonly StringComparison CaseSensitiveComparison = StringComparison.InvariantCulture;
#else
        public static readonly System.StringComparer IngoreCaseComparer = StringComparer.OrdinalIgnoreCase;
        public static readonly StringComparison IngoreCaseComparison = StringComparison.OrdinalIgnoreCase;
         public static readonly StringComparison CaseSensitiveComparison = StringComparison.Ordinal;
#endif
    }


}
