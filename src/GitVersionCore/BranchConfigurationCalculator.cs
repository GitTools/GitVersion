namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    using LibGit2Sharp;

    public class BranchConfigurationCalculator
    {
        private const IncrementStrategy FallbackIncrementStrategy = IncrementStrategy.Minor;

        /// <summary>
        /// Gets the <see cref="BranchConfig"/> for the current commit.
        /// </summary>
        /// <returns>
        /// A KeyValuePair. The key is the name of the branch configuration (from the yaml or the default configs); the value is the actual configuration.
        /// </returns>
        public static BranchConfig GetBranchConfiguration(Commit currentCommit, IRepository repository, bool onlyEvaluateTrackedBranches, Config config, Branch currentBranch, Branch[] excludedBranches)
        {
            var matchingBranches = LookupBranchConfiguration(config, currentBranch).ToArray();

            if (matchingBranches.Length == 0)
            {
                Logger.WriteInfo(string.Format(
                    "No branch configuration found for branch {0}, falling back to default configuration",
                    currentBranch.FriendlyName));

                var branchConfig = new BranchConfig { Name = string.Empty };
                ConfigurationProvider.ApplyBranchDefaults(config, branchConfig, "");
                return branchConfig;
            }

            if (matchingBranches.Length == 1)
            {
                var branchConfiguration = matchingBranches[0];

                if (branchConfiguration.Increment == IncrementStrategy.Inherit)
                {
                    // Use the parent's increment strategy. If none is found, use FallbackIncrementStrategy,
                    var parentIncrement = GetParentIncrementStrategy(onlyEvaluateTrackedBranches, repository, currentCommit, currentBranch, branchConfiguration, config, excludedBranches) ?? FallbackIncrementStrategy;

                    // Overwrite the Increment with the parent's value. That way, no future searching needs to be done.
                    branchConfiguration.Increment = parentIncrement;
                }

                return branchConfiguration;
            }

            const string format = "Multiple branch configurations match the current branch branchName of '{0}'. Matching configurations: '{1}'";
            throw new Exception(string.Format(format, currentBranch.FriendlyName, string.Join(", ", matchingBranches.Select(b => b.Name))));
        }

        static IEnumerable<BranchConfig> LookupBranchConfiguration([NotNull] Config config, [NotNull] Branch currentBranch)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (currentBranch == null)
            {
                throw new ArgumentNullException("currentBranch");
            }

            return config.Branches.Where(b => Regex.IsMatch(currentBranch.FriendlyName, "^" + b.Value.Regex, RegexOptions.IgnoreCase)).Select(kvp => kvp.Value);
        }


        /// <summary>
        /// Recursively look for an IncrementStrategy in the parent branches, which is set and different than <see cref="IncrementStrategy.Inherit"/>.
        /// </summary>
        static IncrementStrategy? GetParentIncrementStrategy(bool onlyEvaluateTrackedBranches, IRepository repository, Commit currentCommit, Branch currentBranch, BranchConfig branchConfiguration, Config config, Branch[] excludedBranches)
        {
            if (branchConfiguration.Increment != IncrementStrategy.Inherit && branchConfiguration.Increment != null)
            {
                // Found an increment strategy which can be used.
                return branchConfiguration.Increment;
            }

            using (Logger.IndentLog("Attempting to get increment value from parent branch"))
            {
                var possibleParents = FindParentBranches(repository, currentCommit, currentBranch, excludedBranches).ToList();

                if (possibleParents.Count == 1)
                {
                    // Single parent found => get its IncrementStrategy.
                    var branchConfig = GetBranchConfiguration(currentCommit, repository, onlyEvaluateTrackedBranches, config, possibleParents[0], excludedBranches);
                    return branchConfig.Increment;
                }

                // Multiple parents found; it is probably because the branch has been merged and we can't do much.
                // So we will fall back to develop's value if develop exists and master if not.
                string errorMessage;
                if (possibleParents.Count == 0)
                    errorMessage = "Failed to inherit Increment branch configuration, no branches found.";
                else
                    errorMessage = "Failed to inherit Increment branch configuration, ended up with: " + string.Join(", ", possibleParents.Select(p => p.FriendlyName));

                var chosenBranch = repository.Branches.FirstOrDefault(b => Regex.IsMatch(b.FriendlyName, "^develop", RegexOptions.IgnoreCase)
                                                                           || Regex.IsMatch(b.FriendlyName, "master$", RegexOptions.IgnoreCase));
                if (chosenBranch == null)
                {
                    // TODO We should call the build server to generate this exception, each build server works differently
                    // for fetch issues and we could give better warnings.
                    throw new InvalidOperationException("Could not find a 'develop' or 'master' branch, neither locally nor remotely.");
                }

                var branchName = chosenBranch.FriendlyName;
                Logger.WriteWarning(errorMessage + Environment.NewLine + Environment.NewLine + "Falling back to " + branchName + " branch config");

                var inheritingBranchConfig = GetBranchConfiguration(currentCommit, repository, onlyEvaluateTrackedBranches, config, chosenBranch, excludedBranches);
                return inheritingBranchConfig.Increment;
            }
        }

        public static IEnumerable<Branch> FindParentBranches(IRepository repository, Commit currentCommit, Branch currentBranch, Branch[] excludedBranches)
        {
            using (Logger.IndentLog("Searching for parent branches"))
            {
                // Find out which branches to exclude as possible parent branches: The current branch, and possibly more in case of a merge commit.
                var newlyExcludedBranches = new[]
                {
                    currentBranch
                };
                // Check if we are a merge commit. If so likely we are a pull request
                var parentCount = currentCommit.Parents.Count();
                if (parentCount == 2)
                {
                    newlyExcludedBranches = CalculateWhenMultipleParents(repository, currentCommit, ref currentBranch, newlyExcludedBranches);
                }

                excludedBranches = excludedBranches == null ? newlyExcludedBranches : excludedBranches.Union(newlyExcludedBranches).ToArray();
                var branchesToEvaluate = repository.Branches.ExcludingBranches(excludedBranches).ToList();

                // Try to find the branch point commit, i.e. the commit where the branch was created from a different branch.
                var branchPoint = currentBranch.FindCommitBranchWasBranchedFrom(repository, excludedBranches);
                if (branchPoint == BranchCommit.Empty)
                {
                    // For whatever reason, no branch point was found.
                    return Enumerable.Empty<Branch>();
                }

                // Return the branch of the branch point commit.
                // TODO Or use the branch where the commit was found?
                var branches = branchPoint.Commit.GetBranchesContainingCommit(repository, branchesToEvaluate, true).ToList();
                if (branches.Count > 1)
                {
                    // The commit is contained in multiple branches => return all, except those also belonging to the commit.
                    var currentTipBranches = currentCommit.GetBranchesContainingCommit(repository, branchesToEvaluate, true);
                    return branches.ExcludingBranches(currentTipBranches);
                }

                return branches;
            }
        }

        static Branch[] CalculateWhenMultipleParents(IRepository repository, Commit currentCommit, ref Branch currentBranch, Branch[] excludedBranches)
        {
            var parents = currentCommit.Parents.ToArray();
            var branches = repository.Branches.Where(b => !b.IsRemote && b.Tip == parents[1]).ToList();
            if (branches.Count == 1)
            {
                var branch = branches[0];
                excludedBranches = new[]
                {
                    currentBranch,
                    branch
                };
                currentBranch = branch;
            }
            else if (branches.Count > 1)
            {
                currentBranch = branches.FirstOrDefault(b => b.FriendlyName == "master") ?? branches.First();
            }
            else
            {
                var possibleTargetBranches = repository.Branches.Where(b => !b.IsRemote && b.Tip == parents[0]).ToList();
                if (possibleTargetBranches.Count > 1)
                {
                    currentBranch = possibleTargetBranches.FirstOrDefault(b => b.FriendlyName == "master") ?? possibleTargetBranches.First();
                }
                else
                {
                    currentBranch = possibleTargetBranches.FirstOrDefault() ?? currentBranch;
                }
            }

            Logger.WriteInfo("HEAD is merge commit, this is likely a pull request using " + currentBranch.FriendlyName + " as base");

            return excludedBranches;
        }
    }
}