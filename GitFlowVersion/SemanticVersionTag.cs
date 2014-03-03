namespace GitFlowVersion
{
    using System;

    public class SemanticVersionTag
    {
        private string name;

        protected bool Equals(SemanticVersionTag other)
        {
            if (other == null)
                return false;
            return string.Equals(name, other.name);
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
            return (name != null ? name.GetHashCode() : 0);
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
            return StringComparer.InvariantCultureIgnoreCase.Compare(left.name, right.name) == 1;
        }

        public static bool operator <(SemanticVersionTag left, SemanticVersionTag right)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(left.name, right.name) == -1;
        }

        public static bool operator >=(SemanticVersionTag left, SemanticVersionTag right)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(left.name, right.name) != -1;
        }

        public static bool operator <=(SemanticVersionTag left, SemanticVersionTag right)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(left.name, right.name) != 1;
        }

        public static implicit operator SemanticVersionTag(string name)
        {
            return new SemanticVersionTag
            {
                name = name
            };
        }

        public static implicit operator string(SemanticVersionTag tag)
        {
            return tag.name;
        }

        public override string ToString()
        {
            return name;
        }

        public bool HasTag()
        {
            return !string.IsNullOrEmpty(name);
        }
    }
}