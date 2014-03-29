namespace GitVersion
{
    using System;

    public class SemanticVersionBuildMetaData : IFormattable
    {
        public readonly int CommitsSinceTag;
        public readonly string Branch;
        public readonly BranchType? BranchType;
        public readonly string Sha;

        public SemanticVersionBuildMetaData()
        {
        }

        public SemanticVersionBuildMetaData(int commitsSinceTag)
        {
            CommitsSinceTag = commitsSinceTag;
        }

        public SemanticVersionBuildMetaData(int commitsSinceTag, string branch, BranchType? branchType, string sha)
        {
            Sha = sha;
            BranchType = branchType;
            CommitsSinceTag = commitsSinceTag;
            Branch = branch;
        }

        protected bool Equals(SemanticVersionBuildMetaData other)
        {
            return CommitsSinceTag == other.CommitsSinceTag && string.Equals(Branch, other.Branch) && string.Equals(BranchType, other.BranchType) && string.Equals(Sha, other.Sha);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((SemanticVersionBuildMetaData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CommitsSinceTag;
                hashCode = (hashCode * 397) ^ (Branch != null ? Branch.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (BranchType != null ? BranchType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Sha != null ? Sha.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToString(null, null);
        }

        /// <summary>
        /// <para>b - Formats just the build number</para>
        /// <para>s - Formats the build number and the Git Sha</para>
        /// <para>o - Formats the full build metadata without the branch type</para>
        /// <para>f - Formats the full build metadata</para>
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (formatProvider != null)
            {
                var formatter = formatProvider.GetFormat(GetType()) as ICustomFormatter;

                if (formatter != null)
                    return formatter.Format(format, this, formatProvider);
            }

            if (string.IsNullOrEmpty(format))
                format = "b";

            switch (format.ToLower())
            {
                case "b":
                    return CommitsSinceTag.ToString();
                case "s":
                    return string.Format("{0}.Sha.{1}", CommitsSinceTag, Sha);
                case "o":
                    return string.Format("{0}.Branch.{1}.Sha.{2}", CommitsSinceTag, Branch, Sha);
                case "f":
                    return string.Format(
                        "{0}.Branch.{1}.BranchType.{2}.Sha.{3}",
                        CommitsSinceTag, Branch, 
                        BranchType == null ? "unknown" : BranchType.Value.ToString(), 
                        Sha);
                default:
                    throw new ArgumentException("Unrecognised format", "format");
            }
        }

        public static bool operator ==(SemanticVersionBuildMetaData left, SemanticVersionBuildMetaData right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SemanticVersionBuildMetaData left, SemanticVersionBuildMetaData right)
        {
            return !Equals(left, right);
        }
    }
}