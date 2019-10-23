using System;

namespace GitVersion.Helpers
{
    // From the LibGit2Sharp project (libgit2sharp.com)
    // MIT License - Copyright (c) 2011-2014 LibGit2Sharp contributors
    // see https://github.com/libgit2/libgit2sharp/blob/7af5c60f22f9bd6064204f84467cfa62bedd1147/LibGit2Sharp/Core/LambdaEqualityHelper.cs
    internal class LambdaEqualityHelper<T>
    {
        private readonly Func<T, object>[] equalityContributorAccessors;

        public LambdaEqualityHelper(params Func<T, object>[] equalityContributorAccessors)
        {
            this.equalityContributorAccessors = equalityContributorAccessors;
        }

        public bool Equals(T instance, T other)
        {
            if (ReferenceEquals(null, instance) || ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(instance, other))
            {
                return true;
            }

            if (instance.GetType() != other.GetType())
            {
                return false;
            }

            foreach (var accessor in equalityContributorAccessors)
            {
                if (!Equals(accessor(instance), accessor(other)))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(T instance)
        {
            var hashCode = GetType().GetHashCode();

            unchecked
            {
                foreach (var accessor in equalityContributorAccessors)
                {
                    var item = accessor(instance);
                    hashCode = (hashCode * 397) ^ (item != null ? item.GetHashCode() : 0);
                }
            }

            return hashCode;
        }
    }
}
