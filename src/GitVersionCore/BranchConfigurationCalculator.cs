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
        public static KeyValuePair<string, BranchConfig> GetBranchConfiguration(Commit currentCommit, IRepository repository, bool onlyEvaluateTrackedBranches, Config config, Branch currentBranch, IList<Branch> excludedInheritBranches = null)
        {
            var matchingBranches = LookupBranchConfiguration(config, currentBranch);

            if (matchingBranches.Length == 0)
            {
                var branchConfig = new BranchConfig();
                ConfigurationProvider.ApplyBranchDefaults(config, branchConfig);
                return new KeyValuePair<string, BranchConfig>(string.Empty, branchConfig);
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
            throw new Exception(string.Format(format, currentBranch.FriendlyName, string.Join(", ", matchingBranches.Select(b => b.Key))));
        }

        static KeyValuePair<string, BranchConfig>[] LookupBranchConfiguration([NotNull] Config config, [NotNull] Branch currentBranch)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            
            if (currentBranch == null)
            {
                throw new ArgumentNullException("currentBranch");
            }
            
            return config.Branches.Where(b => Regex.IsMatch(currentBranch.FriendlyName, "^" + b.Key, RegexOptions.IgnoreCase)).ToArray();
        }


        static KeyValuePair<string, BranchConfig> InheritBranchConfiguration(bool onlyEvaluateTrackedBranches, IRepository repository, Commit currentCommit, Branch currentBranch, KeyValuePair<string, BranchConfig> keyValuePair, BranchConfig branchConfiguration, Config config, IList<Branch> excludedInheritBranches)
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
                        var branchConfig = LookupBranchConfiguration(config, b);

                        // NOTE: if length is 0 we couldn't find the configuration for the branch e.g. "origin/master"
                        // NOTE: if the length is greater than 1 we cannot decide which merge strategy to pick
                        return (branchConfig.Length != 1) || (branchConfig.Length == 1 && branchConfig[0].Value.Increment == IncrementStrategy.Inherit);
                    }).ToList();
                }
                excludedBranches.ToList().ForEach(excludedInheritBranches.Add);
                var branchesToEvaluate = repository.Branches.Except(excludedInheritBranches).ToList();

                var branchPoint = currentBranch.FindCommitBranchWasBranchedFrom(repository, excludedInheritBranches.ToArray());
                List<Branch> possibleParents;
                if (branchPoint == null)
                {
                    possibleParents = currentCommit.GetBranchesContainingCommit(repository, branchesToEvaluate, true)
                        // It fails to inherit Increment branch configuration if more than 1 parent;
                        // therefore no point to get more than 2 parents
                        .Take(2)
                        .ToList();
                }
                else
                {
                    var branches = branchPoint.GetBranchesContainingCommit(repository, branchesToEvaluate, true).ToList();
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
                    var branchConfig = GetBranchConfiguration(currentCommit, repository, onlyEvaluateTrackedBranches, config, possibleParents[0], excludedInheritBranches).Value;
                    return new KeyValuePair<string, BranchConfig>(
                        keyValuePair.Key,
                        new BranchConfig(branchConfiguration)
                        {
                            Increment = branchConfig.Increment,
                            PreventIncrementOfMergedBranchVersion = branchConfig.PreventIncrementOfMergedBranchVersion,
                            // If we are inheriting from develop then we should behave like develop
                            IsDevelop = branchConfig.IsDevelop
                        });
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
                    throw new InvalidOperationException("Could not find a 'develop' or 'master' branch, neither locally nor remotely.");

                var branchName = chosenBranch.FriendlyName;
                Logger.WriteWarning(errorMessage + Environment.NewLine + Environment.NewLine + "Falling back to " + branchName + " branch config");

                var inheritingBranchConfig = GetBranchConfiguration(currentCommit, repository, onlyEvaluateTrackedBranches, config, chosenBranch).Value;
                return new KeyValuePair<string, BranchConfig>(
                    keyValuePair.Key,
                    new BranchConfig(branchConfiguration)
                    {
                        Increment = inheritingBranchConfig.Increment,
                        PreventIncrementOfMergedBranchVersion = inheritingBranchConfig.PreventIncrementOfMergedBranchVersion,
                        // If we are inheriting from develop then we should behave like develop
                        IsDevelop = inheritingBranchConfig.IsDevelop
                    });
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