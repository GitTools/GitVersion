namespace GitFlowVersion
{
    using System;
    using System.Text.RegularExpressions;

    public class SemanticVersionTag
    {
        public string Name;

        public Stability? InferStability()
        {
            if (Name == null)
                return Stability.Final;

            var stageString = Name.TrimEnd("0123456789".ToCharArray());

            if (stageString.Equals("RC", StringComparison.InvariantCultureIgnoreCase))
            {
                return Stability.ReleaseCandidate;
            }

            if (stageString.Equals("hotfix", StringComparison.InvariantCultureIgnoreCase))
            {
                return Stability.Beta;
            }

            Stability stability;
            if (!Enum.TryParse(stageString, true, out stability))
            {
                return null;
            }

            return stability;
        }

        protected bool Equals(SemanticVersionTag other)
        {
            return string.Equals(Name, other.Name);
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
            return Equals((SemanticVersionTag) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public static bool operator ==(SemanticVersionTag left, SemanticVersionTag right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SemanticVersionTag left, SemanticVersionTag right)
        {
            return !Equals(left, right);
        }

        public static bool operator >(SemanticVersionTag left, SemanticVersionTag right)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(left.Name, right.Name) == 1;
        }

        public static bool operator <(SemanticVersionTag left, SemanticVersionTag right)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(left.Name, right.Name) == -1;
        }

        public static bool operator >=(SemanticVersionTag left, SemanticVersionTag right)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(left.Name, right.Name) != -1;
        }

        public static bool operator <=(SemanticVersionTag left, SemanticVersionTag right)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(left.Name, right.Name) != 1;
        }

        public static implicit operator SemanticVersionTag(string name)
        {
            return new SemanticVersionTag
            {
                Name = name
            };
        }

        public bool HasReleaseNumber()
        {
            if (Name == null) return false;
            return Regex.IsMatch(Name, "\\d+");
        }

        public int? ReleaseNumber()
        {
            if (Name == null)
                return null;

            int releaseNumber;
            if (int.TryParse(Regex.Match(Name, "\\d+").Value, out releaseNumber))
                return releaseNumber;

            return 0;
        }
    }
}