using System;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Helpers;

namespace GitVersion
{
    public class SemanticVersionPreReleaseTag :
        IFormattable, IComparable<SemanticVersionPreReleaseTag>, IEquatable<SemanticVersionPreReleaseTag>
    {
        private static readonly LambdaEqualityHelper<SemanticVersionPreReleaseTag> EqualityHelper =
           new LambdaEqualityHelper<SemanticVersionPreReleaseTag>(x => x.Name, x => x.Number);

        public SemanticVersionPreReleaseTag()
        {
        }

        public SemanticVersionPreReleaseTag(string name, int? number)
        {
            Name = name;
            Number = number;
        }

        public SemanticVersionPreReleaseTag(SemanticVersionPreReleaseTag preReleaseTag)
        {
            Name = preReleaseTag.Name;
            Number = preReleaseTag.Number;
        }

        public string Name { get; set; }
        public int? Number { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as SemanticVersionPreReleaseTag);
        }

        public bool Equals(SemanticVersionPreReleaseTag other)
        {
            return EqualityHelper.Equals(this, other);
        }

        public override int GetHashCode()
        {
            return EqualityHelper.GetHashCode(this);
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
            return StringComparerUtils.IgnoreCaseComparer.Compare(left.Name, right.Name) != 1;
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
                // TODO check how to log this
                Console.WriteLine($"Unable to successfully parse semver tag {preReleaseTag}");
                return new SemanticVersionPreReleaseTag();
            }

            var value = match.Groups["name"].Value;
            var number = match.Groups["number"].Success ? int.Parse(match.Groups["number"].Value) : (int?)null;
            if (value.EndsWith("-"))
                return new SemanticVersionPreReleaseTag(preReleaseTag, null);

            return new SemanticVersionPreReleaseTag(value, number);
        }

        public int CompareTo(SemanticVersionPreReleaseTag other)
        {
            if (!HasTag() && other.HasTag())
            {
                return 1;
            }
            if (HasTag() && !other.HasTag())
            {
                return -1;
            }


            var nameComparison = StringComparerUtils.IgnoreCaseComparer.Compare(Name, other.Name);
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
        /// <para>lp - Legacy SemVer tag with the tag number padded. [beta0001]. Can specify an integer to control padding (i.e., lp5)</para>
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider = null)
        {
            if (formatProvider != null)
            {
                if (formatProvider.GetFormat(GetType()) is ICustomFormatter formatter)
                    return formatter.Format(format, this, formatProvider);
            }

            if (string.IsNullOrEmpty(format))
                format = "t";

            format = format.ToLower();
            if (format.StartsWith("lp", StringComparison.Ordinal))
            {
                // Handle format
                var padding = 4;
                if (format.Length > 2)
                {
                    // try to parse
                    if (int.TryParse(format.Substring(2), out var p))
                    {
                        padding = p;
                    }
                }

                return Number.HasValue ? FormatLegacy(GetLegacyName(), Number.Value.ToString("D" + padding)) : FormatLegacy(GetLegacyName());
            }

            return format switch
            {
                "t" => (Number.HasValue ? $"{Name}.{Number}" : Name),
                "l" => (Number.HasValue ? FormatLegacy(GetLegacyName(), Number.Value.ToString()) : FormatLegacy(GetLegacyName())),
                _ => throw new ArgumentException("Unknown format", nameof(format))
            };
        }

        private string FormatLegacy(string tag, string number = "")
        {
            var tagEndsWithANumber = char.IsNumber(tag.Last());
            if (tagEndsWithANumber && number.Length > 0)
                number = "-" + number;

            if (tag.Length + number.Length > 20)
                return $"{tag.Substring(0, 20 - number.Length)}{number}";

            return $"{tag}{number}";
        }

        private string GetLegacyName()
        {
            var firstPart = Name.Split('_')[0];
            return firstPart.Replace(".", string.Empty);
        }

        public bool HasTag()
        {
            return !string.IsNullOrEmpty(Name);
        }
    }
}
