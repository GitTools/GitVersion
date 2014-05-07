namespace GitVersion
{
    using System;
    using System.Text.RegularExpressions;

    public class SemanticVersionBuildMetaData : IFormattable
    {
        static readonly Regex ParseRegex = new Regex(
            @"(?<BuildNumber>\d+)?(\.?Branch(Name)?\.(?<BranchName>[^\.]+))?(\.?Sha?\.(?<Sha>[^\.]+))?(?<Other>.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public int? CommitsSinceTag;
        public string Branch;
        public ReleaseDate ReleaseDate;
        public string Sha;
        public string OtherMetaData;

        public SemanticVersionBuildMetaData()
        {
        }

        public SemanticVersionBuildMetaData(
            int? commitsSinceTag, string branch, ReleaseDate releaseDate)
        {
            ReleaseDate = releaseDate;
            Sha = releaseDate.CommitSha;
            CommitsSinceTag = commitsSinceTag;
            Branch = branch;
        }

        protected bool Equals(SemanticVersionBuildMetaData other)
        {
            return CommitsSinceTag == other.CommitsSinceTag &&
                string.Equals(Branch, other.Branch) &&
                string.Equals(Sha, other.Sha) &&
                Equals(ReleaseDate, other.ReleaseDate);
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
            return Equals((SemanticVersionBuildMetaData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CommitsSinceTag.GetHashCode();
                hashCode = (hashCode*397) ^ (Branch != null ? Branch.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Sha != null ? Sha.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ReleaseDate != null ? ReleaseDate.GetHashCode() : 0);
                return hashCode;
            }
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
                        "{0}{1}{2}{3}",
                        CommitsSinceTag, 
                        string.IsNullOrEmpty(Branch) ? null : ".Branch." + Branch,
                        string.IsNullOrEmpty(Sha) ? null : ".Sha." + Sha,
                        string.IsNullOrEmpty(OtherMetaData) ? null : "." + OtherMetaData)
                        .TrimStart('.');
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

        public static SemanticVersionBuildMetaData Parse(string buildMetaData)
        {
            var semanticVersionBuildMetaData = new SemanticVersionBuildMetaData();
            if (string.IsNullOrEmpty(buildMetaData))
                return semanticVersionBuildMetaData;

            var parsed = ParseRegex.Match(buildMetaData);

            if (parsed.Groups["BuildNumber"].Success)
                semanticVersionBuildMetaData.CommitsSinceTag = int.Parse(parsed.Groups["BuildNumber"].Value);

            if (parsed.Groups["BranchName"].Success)
                semanticVersionBuildMetaData.Branch = parsed.Groups["BranchName"].Value;

            if (parsed.Groups["Sha"].Success)
                semanticVersionBuildMetaData.Sha = parsed.Groups["Sha"].Value;

            if (parsed.Groups["Other"].Success && !string.IsNullOrEmpty(parsed.Groups["Other"].Value))
                semanticVersionBuildMetaData.OtherMetaData = parsed.Groups["Other"].Value.TrimStart('.');

            return semanticVersionBuildMetaData;
        }
    }
}