using System.Collections.Generic;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration
{
    public interface IBranchConfigurationCalculator
    {
        /// <summary>
        /// Gets the <see cref="BranchConfig"/> for the current commit.
        /// </summary>
        BranchConfig GetBranchConfiguration(IBranch targetBranch, Commit currentCommit, Config configuration, IList<IBranch> excludedInheritBranches = null);
    }
}
