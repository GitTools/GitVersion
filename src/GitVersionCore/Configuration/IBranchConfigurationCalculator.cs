using System.Collections.Generic;
using LibGit2Sharp;

namespace GitVersion.Configuration
{
    public interface IBranchConfigurationCalculator
    {
        /// <summary>
        /// Gets the <see cref="BranchConfig"/> for the current commit.
        /// </summary>
        BranchConfig GetBranchConfiguration(IRepository repository, Branch targetBranch, Commit currentCommit, Config configuration, IList<Branch> excludedInheritBranches = null);
    }
}
