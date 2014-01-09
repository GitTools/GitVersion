namespace GitFlowVersion
{
    using System;

    public class SemanticVersionTag
    {
        public string Name;

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


    }
}