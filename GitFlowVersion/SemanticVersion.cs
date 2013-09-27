namespace GitFlowVersion
{
    using System;

    public class SemanticVersion
    {
        public string Suffix;
        public int Major;
        public int Minor;
        public int Patch;
        public int PreReleaseNumber;
        public Stability Stability;


        public bool Equals(SemanticVersion obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Major == obj.Major &&
                   Minor == obj.Minor &&
                   Patch == obj.Patch &&
                   PreReleaseNumber == obj.PreReleaseNumber &&
                   Stability == obj.Stability &&
                   Suffix == obj.Suffix;
        }
        public static bool operator ==(SemanticVersion v1, SemanticVersion v2)
        {
            if (ReferenceEquals(v1, null))
            {
                return ReferenceEquals(v2, null);
            }
            return v1.Equals(v2);
        }
        
        public static bool operator !=(SemanticVersion v1, SemanticVersion v2)
        {
            return !(v1 == v2);
        }
        
        public static bool operator >(SemanticVersion v1, SemanticVersion v2)
        {
            return (v2 < v1);
        }

        public static bool operator >=(SemanticVersion v1, SemanticVersion v2)
        {
            return (v2 <= v1);
        }

        public static bool operator <=(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
            {
                throw new ArgumentNullException("v1");
            }
            return (v1.CompareTo(v2) <= 0);
        }

        public static bool operator <(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
            {
                throw new ArgumentNullException("v1");
            }
            return (v1.CompareTo(v2) < 0);
        }

        public int CompareTo(SemanticVersion value)
        {
            if (value == null)
            {
                return 1;
            }
            if (Major != value.Major)
            {
                if (Major > value.Major)
                {
                    return 1;
                }
                return -1;
            }
            if (Minor != value.Minor)
            {
                if (Minor > value.Minor)
                {
                    return 1;
                }
                return -1;
            }
            if (Patch != value.Patch)
            {
                if (Patch > value.Patch)
                {
                    return 1;
                }
                return -1;
            }
            if (Stability != value.Stability)
            {
                if (Stability > value.Stability)
                {
                    return 1;
                }
                return -1;
            }
            if (PreReleaseNumber != value.PreReleaseNumber)
            {
                if (PreReleaseNumber > value.PreReleaseNumber)
                {
                    return 1;
                }
                return -1;
            }
            return -1;
        }


    }
}