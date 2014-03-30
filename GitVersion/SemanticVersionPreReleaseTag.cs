namespace GitVersion
{
    using System;
    using System.Text.RegularExpressions;

    public class SemanticVersionPreReleaseTag : IFormattable, IComparable<SemanticVersionPreReleaseTag>
    {

        public SemanticVersionPreReleaseTag()
        {
        }

        public SemanticVersionPreReleaseTag(string name, int? number)
        {
            Name = name;
            Number = number;
        }

        public readonly string Name;

        public readonly int? Number;

        protected bool Equals(SemanticVersionPreReleaseTag other)
        {
            return string.Equals(Name, other.Name) && Number == other.Number;
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
            return Equals((SemanticVersionPreReleaseTag) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0)*397) ^ Number.GetHashCode();
            }
        }

        public static bool operator ==(SemanticVersionPreReleaseTag left, SemanticVersionPreReleaseTag right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SemanticVersionPreReleaseTag left, SemanticVersionPreReleaseTag right)
        {
            return !Equals(left, right);
        }

        public static bool operator >(SemanticVersionPreReleaseTag left, SemanticVersionPreReleaseTag right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <(SemanticVersionPreReleaseTag left, SemanticVersionPreReleaseTag right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >=(SemanticVersionPreReleaseTag left, SemanticVersionPreReleaseTag right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <=(SemanticVersionPreReleaseTag left, SemanticVersionPreReleaseTag right)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(left.Name, right.Name) != 1;
        }

        public static implicit operator string(SemanticVersionPreReleaseTag preReleaseTag)
        {
            return preReleaseTag.ToString();
        }

        public static implicit operator SemanticVersionPreReleaseTag(string preReleaseTag)
        {
            return Parse(preReleaseTag);
        }

        public static SemanticVersionPreReleaseTag Parse(string preReleaseTag)
        {
            if (string.IsNullOrEmpty(preReleaseTag))
            {
                return new SemanticVersionPreReleaseTag();
            }

            var match = Regex.Match(preReleaseTag, @"(?<name>.*?)\.?(?<number>\d+)?$");
            if (!match.Success)
            {
                Logger.WriteWarning(string.Format("Unable to successfully parse semver tag {0}", preReleaseTag));
                return new SemanticVersionPreReleaseTag();
            }

            return new SemanticVersionPreReleaseTag(match.Groups["name"].Value, 
                match.Groups["number"].Success ? int.Parse(match.Groups["number"].Value) : (int?) null);
        }

        public int CompareTo(SemanticVersionPreReleaseTag other)
        {
            var nameComparison = StringComparer.InvariantCultureIgnoreCase.Compare(Name, other);
            if (nameComparison != 0)
                return nameComparison;

            return Nullable.Compare(Number, other.Number);
        }

        public override string ToString()
        {
            return ToString(null);
        }

        /// <summary>
        /// Default formats:
        /// <para>t - SemVer 2.0 formatted tag [beta.1]</para>
        /// <para>p - SemVer 2.0 tag with the tag number padded. [beta.0001]</para>
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
                format = "t";

            switch (format.ToLower())
            {
                case "t":
                    return Number.HasValue ? string.Format("{0}.{1}", Name, Number) : Name;
                case "p":
                    return Number.HasValue ? string.Format("{0}.{1}", Name, Number.Value.ToString("D4")) : Name;
                default:
                    throw new ArgumentException("Unknown format", "format");
            }
        }

        public bool HasTag()
        {
            return !string.IsNullOrEmpty(Name);
        }
    }
}