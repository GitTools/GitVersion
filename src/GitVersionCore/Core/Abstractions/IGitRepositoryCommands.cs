using System.Collections.Generic;
using LibGit2Sharp;

namespace GitVersion
{
    /// <summary>
    /// Mockable and testable interface wrapper for the <c>static</c>
    /// <see cref="Commands"/> <c>class</c>.
    /// </summary>
    public interface IGitRepositoryCommands
    {
        Branch Checkout(string committishOrBranchSpec);
        Branch Checkout(Branch branch);
        void Fetch(string remote, IEnumerable<string> refspecs, FetchOptions options, string logMessage);
    }
}
