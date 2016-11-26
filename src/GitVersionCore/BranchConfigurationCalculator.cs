namespace GitVersion
{
    using JetBrains.Annotations;
    using LibGit2Sharp;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class BranchConfigurationCalculator
    {
        /// <summary>
        /// Gets the <see cref="BranchConfig"/> for the current commit.
        /// </summary>
        public static BranchConfig GetBranchConfiguration(Commit currentCommit, IRepository repository, bool onlyEvaluateTrackedBranches, Config config, Branch currentBranch, IList<Branch> excludedInheritBranches = null)
        {
            var matchingBranches = LookupBranchConfiguration(config, currentBranch).ToArray();

            BranchConfig branchConfiguration;
            if (matchingBranches.Length > 0)
            {
                branchConfiguration = matchingBranches[0];

                if (matchingBranches.Length > 1)
                {
                    Logger.WriteWarning(string.Format(
                        "Multiple branch configurations match the current branch branchName of '{0}'. Using the first matching configuration, '{1}'. Matching configurations include: '{2}'",
                        currentBranch.FriendlyName,
                        branchConfiguration.Name,
                        string.Join("', '", matchingBranches.Select(b => b.Name))));
                }
            }
            else
            {
                Logger.WriteInfo(string.Format(
                    "No branch configuration found for branch {0}, falling back to default configuration",
                    currentBranch.FriendlyName));

                branchConfiguration = new BranchConfig { Name = string.Empty };
                ConfigurationProvider.ApplyBranchDefaults(config, branchConfiguration, "");
            }

            return branchConfiguration.Increment == IncrementStrategy.Inherit ?
                InheritBranchConfiguration(onlyEvaluateTrackedBranches, repository, currentCommit, currentBranch, branchConfiguration, config, excludedInheritBranches) :
                branchConfiguration;
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

        static BranchConfig InheritBranchConfiguration(bool onlyEvaluateTrackedBranches, IRepository repository, Commit currentCommit, Branch currentBranch, BranchConfig branchConfiguration, Config config, IList<Branch> excludedInheritBranches)
        {
            using (Logger.IndentLog("Attempting to inherit branch configuration from parent branch"))
            {
                var excludedBranches = new[] { currentBranch };
                // Check if we are a merge commit. If so likely we are a pull request
                var parentCount = currentCommit.Parents.Count();
                if (parentCount == 2)
                {
                    excludedBranches = CalculateWhenMultipleParents(repository, currentCommit, ref currentBranch, excludedBranches);
                }

                if (excludedInheritBranches == null)
                {
                    excludedInheritBranches = repository.Branches.Where(b =>
                    {
                        var branchConfig = LookupBranchConfiguration(config, b).ToArray();

                        // NOTE: if length is 0 we couldn't find the configuration for the branch e.g. "origin/master"
                        // NOTE: if the length is greater than 1 we cannot decide which merge strategy to pick
                        return (branchConfig.Length != 1) || (branchConfig.Length == 1 && branchConfig[0].Increment == IncrementStrategy.Inherit);
                    }).ToList();
                }
                // Add new excluded branches.
                foreach (var excludedBranch in excludedBranches.ExcludingBranches(excludedInheritBranches))
                {
                    excludedInheritBranches.Add(excludedBranch);
                }
                var branchesToEvaluate = repository.Branches.Except(excludedInheritBranches).ToList();

                var branchPoint = currentBranch.FindCommitBranchWasBranchedFrom(repository, excludedInheritBranches.ToArray());
                List<Branch> possibleParents;
                if (branchPoint == BranchCommit.Empty)
                {
                    possibleParents = currentCommit.GetBranchesContainingCommit(repository, branchesToEvaluate, true)
                        // It fails to inherit Increment branch configuration if more than 1 parent;
                        // therefore no point to get more than 2 parents
                        .Take(2)
                        .ToList();
                }
                else
                {
                    var branches = branchPoint.Commit.GetBranchesContainingCommit(repository, branchesToEvaluate, true).ToList();
                    if (branches.Count > 1)
                    {
                        var currentTipBranches = currentCommit.GetBranchesContainingCommit(repository, branchesToEvaluate, true).ToList();
                        possibleParents = branches.Except(currentTipBranches).ToList();
                    }
                    else
                    {
                        possibleParents = branches;
                    }
                }

                Logger.WriteInfo("Found possible parent branches: " + string.Join(", ", possibleParents.Select(p => p.FriendlyName)));

                if (possibleParents.Count == 1)
                {
                    var branchConfig = GetBranchConfiguration(currentCommit, repository, onlyEvaluateTrackedBranches, config, possibleParents[0], excludedInheritBranches);
                    return new BranchConfig(branchConfiguration)
                    {
                        Increment = branchConfig.Increment,
                        PreventIncrementOfMergedBranchVersion = branchConfig.PreventIncrementOfMergedBranchVersion,
                        // If we are inheriting from develop then we should behave like develop
                        TracksReleaseBranches = branchConfig.TracksReleaseBranches
                    };
                }

                // If we fail to inherit it is probably because the branch has been merged and we can't do much. So we will fall back to develop's config
                // if develop exists and master if not
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

                // To prevent infinite loops, make sure that a new branch was chosen.
                if (LibGitExtensions.IsSameBranch(currentBranch, chosenBranch))
                {
                    Logger.WriteWarning("Fallback branch wants to inherit Increment branch configuration from itself. Using patch increment instead.");
                    return new BranchConfig(branchConfiguration)
                    {
                        Increment = IncrementStrategy.Patch
                    };
                }

                var inheritingBranchConfig = GetBranchConfiguration(currentCommit, repository, onlyEvaluateTrackedBranches, config, chosenBranch, excludedInheritBranches);
                return new BranchConfig(branchConfiguration)
                {
                    Increment = inheritingBranchConfig.Increment,
                    PreventIncrementOfMergedBranchVersion = inheritingBranchConfig.PreventIncrementOfMergedBranchVersion,
                    // If we are inheriting from develop then we should behave like develop
                    TracksReleaseBranches = inheritingBranchConfig.TracksReleaseBranches
                };
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