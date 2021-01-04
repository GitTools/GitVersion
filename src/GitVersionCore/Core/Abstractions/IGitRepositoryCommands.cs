using System.Collections.Generic;

namespace GitVersion
{
    /// <summary>
    /// Mockable and testable interface wrapper for the <c>static</c>
    /// </summary>
    public interface IGitRepositoryCommands
    {
        void Checkout(string committishOrBranchSpec);
        void Checkout(Branch branch);
        void Fetch(string remote, IEnumerable<string> refspecs, AuthenticationInfo auth, string logMessage);
    }
}
