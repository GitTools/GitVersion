namespace GitVersion
{
    using System;

    public class SemanticVersionBuildMetaData : IFormattable
    {
        public readonly int? CommitsSinceTag;
        public readonly string Branch;
        public readonly string Sha;

        public SemanticVersionBuildMetaData()
        {
        }

        public SemanticVersionBuildMetaData(int? commitsSinceTag)
        {
            CommitsSinceTag = commitsSinceTag;
        }

        public SemanticVersionBuildMetaData(int? commitsSinceTag, string branch, string sha)
        {
            Sha = sha;
            CommitsSinceTag = commitsSinceTag;
            Branch = branch;
        }

        protected bool Equals(SemanticVersionBuildMetaData other)
        {
            return CommitsSinceTag == other.CommitsSinceTag && string.Equals(Branch, other.Branch) && string.Equals(Sha, other.Sha);
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

        public override string ToString()
        {
            return ToString(null);
        }

        /// <summary>
        /// <para>b - Formats just the build number</para>
        /// <para>s - Formats the build number and the Git Sha</para>
        /// <para>f - Formats the full build metadata</para>
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider = null)
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
                    return string.Format("{0}{1}", CommitsSinceTag, string.IsNullOrEmpty(Sha) ? null : ".Sha." + Sha).TrimStart('.');
                case "f":
                    return string.Format(
                        "{0}{1}{2}",
                        CommitsSinceTag, 
                        string.IsNullOrEmpty(Branch) ? null : ".Branch." + Branch,
                        string.IsNullOrEmpty(Sha) ? null : ".Sha." + Sha).TrimStart('.');
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

        public static implicit operator string(SemanticVersionBuildMetaData preReleaseTag)
        {
            return preReleaseTag.ToString();
        }

        public static implicit operator SemanticVersionBuildMetaData(string preReleaseTag)
        {
            return Parse(preReleaseTag);
        }

        public static implicit operator SemanticVersionBuildMetaData(int numberOfCommitsSinceLastTag)
        {
            //TODO not sure about this implicit cast?
            return new SemanticVersionBuildMetaData(numberOfCommitsSinceLastTag);
        }

        static SemanticVersionBuildMetaData Parse(string buildMetaData)
        {
            //TODO Parse
            return new SemanticVersionBuildMetaData();
        }
    }
}