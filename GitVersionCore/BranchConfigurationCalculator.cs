namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using LibGit2Sharp;

    public class BranchConfigurationCalculator
    {
        public static KeyValuePair<string, BranchConfig> GetBranchConfiguration(Commit currentCommit, IRepository repository, bool onlyEvaluateTrackedBranches, Config config, Branch currentBranch, IList<Branch> excludedInheritBranches = null)
        {
            var matchingBranches = LookupBranchConfiguration(config, currentBranch);

            if (matchingBranches.Length == 0)
            {
                return new KeyValuePair<string, BranchConfig>(string.Empty, new BranchConfig());
            }
            if (matchingBranches.Length == 1)
            {
                var keyValuePair = matchingBranches[0];
                var branchConfiguration = keyValuePair.Value;

                if (branchConfiguration.Increment == IncrementStrategy.Inherit)
                {
                    return InheritBranchConfiguration(onlyEvaluateTrackedBranches, repository, currentCommit, currentBranch, keyValuePair, branchConfiguration, config, excludedInheritBranches);
                }

                return keyValuePair;
            }

            const string format = "Multiple branch configurations match the current branch branchName of '{0}'. Matching configurations: '{1}'";
            throw new Exception(string.Format(format, currentBranch.Name, string.Join(", ", matchingBranches.Select(b => b.Key))));
        }

        static KeyValuePair<string, BranchConfig>[] LookupBranchConfiguration(Config config, Branch currentBranch)
        {
            return config.Branches.Where(b => Regex.IsMatch(currentBranch.Name, "^" + b.Key, RegexOptions.IgnoreCase)).ToArray();
        }

        static KeyValuePair<string, BranchConfig> InheritBranchConfiguration(bool onlyEvaluateTrackedBranches, IRepository repository, Commit currentCommit, Branch currentBranch, KeyValuePair<string, BranchConfig> keyValuePair, BranchConfig branchConfiguration, Config config, IList<Branch> excludedInheritBranches)
        {
            Logger.WriteInfo("Attempting to inherit branch configuration from parent branch");
            var excludedBranches = new [] { currentBranch };
            // Check if we are a merge commit. If so likely we are a pull request
            var parentCount = currentCommit.Parents.Count();
            if (parentCount == 2)
            {
                var parents = currentCommit.Parents.ToArray();
                var branch = repository.Branches.SingleOrDefault(b => !b.IsRemote && b.Tip == parents[1]);
                if (branch != null)
                {
                    excludedBranches = new[]
                    {
                        currentBranch,
                        branch
                    };
                    currentBranch = branch;
                }
                else
                {
                    var possibleTargetBranches = repository.Branches.Where(b => !b.IsRemote && b.Tip == parents[0]).ToList();
                    if (possibleTargetBranches.Count() > 1)
                    {
                        currentBranch = possibleTargetBranches.FirstOrDefault(b => b.Name == "master") ?? possibleTargetBranches.First();
                    }
                    else
                    {
                        currentBranch = possibleTargetBranches.FirstOrDefault() ?? currentBranch;
                    }
                }

                Logger.WriteInfo("HEAD is merge commit, this is likely a pull request using " + currentBranch.Name + " as base");
            }
            if (excludedInheritBranches == null)
            {
                excludedInheritBranches = repository.Branches.Where(b =>
                {
                    var branchConfig = LookupBranchConfiguration(config, b);
                    return branchConfig.Length == 1 && branchConfig[0].Value.Increment == IncrementStrategy.Inherit;
                }).ToList();
            }
            excludedBranches.ToList().ForEach(excludedInheritBranches.Add);

            var branchPoint = currentBranch.FindCommitBranchWasBranchedFrom(repository, excludedInheritBranches.ToArray());

            List<Branch> possibleParents;
            if (branchPoint == null)
            {
                possibleParents = currentCommit.GetBranchesContainingCommit(repository, true).Except(excludedInheritBranches).ToList();
            }
            else
            {
                var branches = branchPoint.GetBranchesContainingCommit(repository, true).Except(excludedInheritBranches).ToList();
                if (branches.Count > 1)
                {
                    var currentTipBranches = currentCommit.GetBranchesContainingCommit(repository, true).Except(excludedInheritBranches).ToList();
                    possibleParents = branches.Except(currentTipBranches).ToList();
                }
                else
                {
                    possibleParents = branches;
                }
            }

            Logger.WriteInfo("Found possible parent branches: " + string.Join(", ", possibleParents.Select(p => p.Name)));

            if (possibleParents.Count == 1)
            {
                var branchConfig = GetBranchConfiguration(currentCommit, repository, onlyEvaluateTrackedBranches, config, possibleParents[0], excludedInheritBranches).Value;
                return new KeyValuePair<string, BranchConfig>(
                    keyValuePair.Key,
                    new BranchConfig(branchConfiguration)
                    {
                        Increment = branchConfig.Increment,
                        PreventIncrementOfMergedBranchVersion = branchConfig.PreventIncrementOfMergedBranchVersion
                    });
            }

            // If we fail to inherit it is probably because the branch has been merged and we can't do much. So we will fall back to develop's config
            // if develop exists and master if not
            string errorMessage;
            if (possibleParents.Count == 0)
                errorMessage = "Failed to inherit Increment branch configuration, no branches found.";
            else
                errorMessage = "Failed to inherit Increment branch configuration, ended up with: " + string.Join(", ", possibleParents.Select(p => p.Name));

            var developBranch = repository.Branches.FirstOrDefault(b => Regex.IsMatch(b.Name, "^develop", RegexOptions.IgnoreCase));
            var branchName = developBranch != null ? developBranch.Name : "master";

            Logger.WriteWarning(errorMessage + Environment.NewLine + Environment.NewLine + "Falling back to " + branchName + " branch config");
            var value = GetBranchConfiguration(currentCommit, repository, onlyEvaluateTrackedBranches, config, repository.Branches[branchName]).Value;
            return new KeyValuePair<string, BranchConfig>(
                keyValuePair.Key,
                new BranchConfig(branchConfiguration)
                {
                    Increment = value.Increment,
                    PreventIncrementOfMergedBranchVersion = value.PreventIncrementOfMergedBranchVersion
                });
        }
    }
}