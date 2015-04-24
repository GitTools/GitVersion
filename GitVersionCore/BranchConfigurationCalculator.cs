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
            var matchingBranches = config.Branches.Where(b => Regex.IsMatch(currentBranch.Name, "^" + b.Key, RegexOptions.IgnoreCase)).ToArray();

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
                excludedInheritBranches = new List<Branch>();
            }
            excludedBranches.ToList().ForEach(excludedInheritBranches.Add);

            var branchPoint = currentBranch.FindCommitBranchWasBranchedFrom(repository, onlyEvaluateTrackedBranches, excludedInheritBranches.ToArray());

            List<Branch> possibleParents;
            if (branchPoint.Sha == currentCommit.Sha)
            {
                possibleParents = currentCommit.GetBranchesContainingCommit(repository, true).Except(excludedInheritBranches).ToList();
            }
            else
            {
                var branches = branchPoint.GetBranchesContainingCommit(repository, true).Except(excludedInheritBranches).ToList();
                var currentTipBranches = currentCommit.GetBranchesContainingCommit(repository, true).Except(excludedInheritBranches).ToList();
                possibleParents = branches
                    .Except(currentTipBranches)
                    .ToList();
            }

            Logger.WriteInfo("Found possible parent branches: " + string.Join(", ", possibleParents.Select(p => p.Name)));

            // If it comes down to master and something, master is always first so we pick other branch
            if (possibleParents.Count == 2 && possibleParents.Any(p => p.Name == "master"))
            {
                possibleParents.Remove(possibleParents.Single(p => p.Name == "master"));
            }

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

            var hasDevelop = repository.Branches["develop"] != null;
            var branchName = hasDevelop ? "develop" : "master";

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