namespace GitVersion
{
    using System.Collections.Generic;
    using LibGit2Sharp;

    class BranchNameComparer : IEqualityComparer<Branch>
    {
        public bool Equals(Branch x, Branch y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(Branch obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}