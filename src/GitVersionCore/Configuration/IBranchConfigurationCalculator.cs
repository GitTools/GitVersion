using System.Collections.Generic;
using GitVersion.Model.Configuration;
using LibGit2Sharp;

namespace GitVersion.Configuration
{
    public interface IBranchConfigurationCalculator
    {
        /// <summary>
        /// Gets the <see cref="BranchConfig"/> for the current commit.
        /// </summary>
        BranchConfig GetBranchConfiguration(Branch targetBranch, Commit currentCommit, Config configuration, IList<Branch> excludedInheritBranches = null);
    }
}
