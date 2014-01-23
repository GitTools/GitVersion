namespace GitFlowVersion
{
    using System;

    public class SemanticVersionTag
    {
        private string _name;

        protected bool Equals(SemanticVersionTag other)
        {
            return string.Equals(_name, other._name);
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
            return (_name != null ? _name.GetHashCode() : 0);
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
            return StringComparer.InvariantCultureIgnoreCase.Compare(left._name, right._name) == 1;
        }

        public static bool operator <(SemanticVersionTag left, SemanticVersionTag right)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(left._name, right._name) == -1;
        }

        public static bool operator >=(SemanticVersionTag left, SemanticVersionTag right)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(left._name, right._name) != -1;
        }

        public static bool operator <=(SemanticVersionTag left, SemanticVersionTag right)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(left._name, right._name) != 1;
        }

        public static implicit operator SemanticVersionTag(string name)
        {
            return new SemanticVersionTag
            {
                _name = name
            };
        }

        public static implicit operator string(SemanticVersionTag tag)
        {
            return tag._name;
        }

        public override string ToString()
        {
            return _name;
        }

        public bool HasTag()
        {
            return !string.IsNullOrEmpty(_name);
        }
    }
}