namespace GitVersion
{
    using System;
    using System.Text.RegularExpressions;

    public class SemanticVersionPreReleaseTag : 
        IFormattable, IComparable<SemanticVersionPreReleaseTag>, IEquatable<SemanticVersionPreReleaseTag>
    {
        public string Name;
        public int? Number;

        private static readonly LambdaEqualityHelper<SemanticVersionPreReleaseTag> equalityHelper =
           new LambdaEqualityHelper<SemanticVersionPreReleaseTag>(x => x.Name, x => x.Number);

        public SemanticVersionPreReleaseTag()
        {
        }

        public SemanticVersionPreReleaseTag(string name, int? number)
        {
            Name = name;
            Number = number;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SemanticVersionPreReleaseTag);
        }

        public bool Equals(SemanticVersionPreReleaseTag other)
        {
            return equalityHelper.Equals(this, other);
        }

        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
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
        /// <para>l - Legacy SemVer tag with the tag number padded. [beta1]</para>
        /// <para>lp - Legacy SemVer tag with the tag number padded. [beta0001]</para>
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
                case "l":
                    return Number.HasValue ? FormatLegacy(GetLegacyName(), Number.ToString()) : FormatLegacy(GetLegacyName());
                case "lp":
                    return Number.HasValue ? FormatLegacy(GetLegacyName(), Number.Value.ToString("D4")) : FormatLegacy(GetLegacyName());
                default:
                    throw new ArgumentException("Unknown format", "format");
            }
        }

        string FormatLegacy(string tag, string number = null)
        {
            var tagLength = tag.Length;
            var numberLength = number == null ? 0 : number.Length;

            if (tagLength + numberLength > 20)
                return string.Format("{0}{1}", tag.Substring(0, 20 - numberLength), number);

            return string.Format("{0}{1}", tag, number);
        }

        string GetLegacyName()
        {
            var firstPart = Name.Split('_')[0];
            return firstPart.Replace("-", string.Empty).Replace(".", string.Empty);
        }

        public bool HasTag()
        {
            return !string.IsNullOrEmpty(Name);
        }
    }
}
