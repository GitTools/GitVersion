using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Common;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using LibGit2Sharp;

namespace GitVersion.Configuration
{
    public class BranchConfigurationCalculator : IBranchConfigurationCalculator
    {
        private const string FallbackConfigName = "Fallback";

        private readonly ILog log;
        private readonly IRepositoryMetadataProvider repositoryMetadataProvider;

        public BranchConfigurationCalculator(ILog log, IRepositoryMetadataProvider repositoryMetadataProvider)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.repositoryMetadataProvider = repositoryMetadataProvider ?? throw new ArgumentNullException(nameof(repositoryMetadataProvider));
        }

        /// <summary>
        /// Gets the <see cref="BranchConfig"/> for the current commit.
        /// </summary>
        public BranchConfig GetBranchConfiguration(Branch targetBranch, Commit currentCommit, Config configuration, IList<Branch> excludedInheritBranches = null)
        {
            var matchingBranches = configuration.GetConfigForBranch(targetBranch.NameWithoutRemote());

            if (matchingBranches == null)
            {
                log.Info($"No branch configuration found for branch {targetBranch.FriendlyName}, falling back to default configuration");

                matchingBranches = new BranchConfig { Name = FallbackConfigName };
                configuration.ApplyBranchDefaults(matchingBranches, "", new List<string>());
            }

            if (matchingBranches.Increment == IncrementStrategy.Inherit)
            {
                matchingBranches = InheritBranchConfiguration(targetBranch, matchingBranches, currentCommit, configuration, excludedInheritBranches);
                if (matchingBranches.Name == FallbackConfigName && matchingBranches.Increment == IncrementStrategy.Inherit)
                {
                    // We tried, and failed to inherit, just fall back to patch
                    matchingBranches.Increment = IncrementStrategy.Patch;
                }
            }

            return matchingBranches;
        }

        // TODO I think we need to take a fresh approach to this.. it's getting really complex with heaps of edge cases
        private BranchConfig InheritBranchConfiguration(Branch targetBranch, BranchConfig branchConfiguration, Commit currentCommit, Config configuration, IList<Branch> excludedInheritBranches)
        {
            using (log.IndentLog("Attempting to inherit branch configuration from parent branch"))
            {
                var excludedBranches = new[] { targetBranch };
                // Check if we are a merge commit. If so likely we are a pull request
                var parentCount = currentCommit.Parents.Count();
                if (parentCount == 2)
                {
                    excludedBranches = CalculateWhenMultipleParents(currentCommit, ref targetBranch, excludedBranches);
                }

                excludedInheritBranches ??= repositoryMetadataProvider.GetExcludedInheritBranches(configuration);
                // Add new excluded branches.
                foreach (var excludedBranch in excludedBranches.ExcludingBranches(excludedInheritBranches))
                {
                    excludedInheritBranches.Add(excludedBranch);
                }
                var branchesToEvaluate = repositoryMetadataProvider.ExcludingBranches(excludedInheritBranches).ToList();

                var branchPoint = repositoryMetadataProvider
                    .FindCommitBranchWasBranchedFrom(targetBranch, configuration, excludedInheritBranches.ToArray());
                List<Branch> possibleParents;
                if (branchPoint == BranchCommit.Empty)
                {
                    possibleParents = repositoryMetadataProvider.GetBranchesContainingCommit(targetBranch.Tip, branchesToEvaluate)
                        // It fails to inherit Increment branch configuration if more than 1 parent;
                        // therefore no point to get more than 2 parents
                        .Take(2)
                        .ToList();
                }
                else
                {
                    var branches = repositoryMetadataProvider.GetBranchesContainingCommit(branchPoint.Commit, branchesToEvaluate).ToList();
                    if (branches.Count > 1)
                    {
                        var currentTipBranches = repositoryMetadataProvider.GetBranchesContainingCommit(currentCommit, branchesToEvaluate).ToList();
                        possibleParents = branches.Except(currentTipBranches).ToList();
                    }
                    else
                    {
                        possibleParents = branches;
                    }
                }

                log.Info("Found possible parent branches: " + string.Join(", ", possibleParents.Select(p => p.FriendlyName)));

                if (possibleParents.Count == 1)
                {
                    var branchConfig = GetBranchConfiguration(possibleParents[0], currentCommit, configuration, excludedInheritBranches);
                    // If we have resolved a fallback config we should not return that we have got config
                    if (branchConfig.Name != FallbackConfigName)
                    {
                        return new BranchConfig(branchConfiguration)
                        {
                            Increment = branchConfig.Increment,
                            PreventIncrementOfMergedBranchVersion = branchConfig.PreventIncrementOfMergedBranchVersion,
                            // If we are inheriting from develop then we should behave like develop
                            TracksReleaseBranches = branchConfig.TracksReleaseBranches
                        };
                    }
                }

                // If we fail to inherit it is probably because the branch has been merged and we can't do much. So we will fall back to develop's config
                // if develop exists and master if not
                var errorMessage = possibleParents.Count == 0
                    ? "Failed to inherit Increment branch configuration, no branches found."
                    : "Failed to inherit Increment branch configuration, ended up with: " + string.Join(", ", possibleParents.Select(p => p.FriendlyName));

                var chosenBranch = repositoryMetadataProvider.GetChosenBranch(configuration);
                if (chosenBranch == null)
                {
                    // TODO We should call the build server to generate this exception, each build server works differently
                    // for fetch issues and we could give better warnings.
                    throw new InvalidOperationException("Could not find a 'develop' or 'master' branch, neither locally nor remotely.");
                }

                var branchName = chosenBranch.FriendlyName;
                log.Warning(errorMessage + System.Environment.NewLine + "Falling back to " + branchName + " branch config");

                // To prevent infinite loops, make sure that a new branch was chosen.
                if (targetBranch.IsSameBranch(chosenBranch))
                {
                    var developOrMasterConfig =
                        ChooseMasterOrDevelopIncrementStrategyIfTheChosenBranchIsOneOfThem(
                            chosenBranch, branchConfiguration, configuration);
                    if (developOrMasterConfig != null)
                    {
                        return developOrMasterConfig;
                    }

                    log.Warning("Fallback branch wants to inherit Increment branch configuration from itself. Using patch increment instead.");
                    return new BranchConfig(branchConfiguration)
                    {
                        Increment = IncrementStrategy.Patch
                    };
                }

                var inheritingBranchConfig = GetBranchConfiguration(chosenBranch, currentCommit, configuration, excludedInheritBranches);
                var configIncrement = inheritingBranchConfig.Increment;
                if (inheritingBranchConfig.Name == FallbackConfigName && configIncrement == IncrementStrategy.Inherit)
                {
                    log.Warning("Fallback config inherits by default, dropping to patch increment");
                    configIncrement = IncrementStrategy.Patch;
                }
                return new BranchConfig(branchConfiguration)
                {
                    Increment = configIncrement,
                    PreventIncrementOfMergedBranchVersion = inheritingBranchConfig.PreventIncrementOfMergedBranchVersion,
                    // If we are inheriting from develop then we should behave like develop
                    TracksReleaseBranches = inheritingBranchConfig.TracksReleaseBranches
                };
            }
        }

        private Branch[] CalculateWhenMultipleParents(Commit currentCommit, ref Branch currentBranch, Branch[] excludedBranches)
        {
            var parents = currentCommit.Parents.ToArray();
            var branches = repositoryMetadataProvider.GetBranchesForCommit(parents[1]);
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
                currentBranch = branches.FirstOrDefault(b => b.NameWithoutRemote() == "master") ?? branches.First();
            }
            else
            {
                var possibleTargetBranches = repositoryMetadataProvider.GetBranchesForCommit(parents[0]);
                if (possibleTargetBranches.Count > 1)
                {
                    currentBranch = possibleTargetBranches.FirstOrDefault(b => b.NameWithoutRemote() == "master") ?? possibleTargetBranches.First();
                }
                else
                {
                    currentBranch = possibleTargetBranches.FirstOrDefault() ?? currentBranch;
                }
            }

            log.Info("HEAD is merge commit, this is likely a pull request using " + currentBranch.FriendlyName + " as base");

            return excludedBranches;
        }



        private static BranchConfig ChooseMasterOrDevelopIncrementStrategyIfTheChosenBranchIsOneOfThem(Branch chosenBranch, BranchConfig branchConfiguration, Config config)
        {
            BranchConfig masterOrDevelopConfig = null;
            var developBranchRegex = config.Branches[Config.DevelopBranchKey].Regex;
            var masterBranchRegex = config.Branches[Config.MasterBranchKey].Regex;
            if (Regex.IsMatch(chosenBranch.FriendlyName, developBranchRegex, RegexOptions.IgnoreCase))
            {
                // Normally we would not expect this to happen but for safety we add a check
                if (config.Branches[Config.DevelopBranchKey].Increment !=
                    IncrementStrategy.Inherit)
                {
                    masterOrDevelopConfig = new BranchConfig(branchConfiguration)
                    {
                        Increment = config.Branches[Config.DevelopBranchKey].Increment
                    };
                }
            }
            else if (Regex.IsMatch(chosenBranch.FriendlyName, masterBranchRegex, RegexOptions.IgnoreCase))
            {
                // Normally we would not expect this to happen but for safety we add a check
                if (config.Branches[Config.MasterBranchKey].Increment !=
                    IncrementStrategy.Inherit)
                {
                    masterOrDevelopConfig = new BranchConfig(branchConfiguration)
                    {
                        Increment = config.Branches[Config.DevelopBranchKey].Increment
                    };
                }
            }
            return masterOrDevelopConfig;
        }
    }
}
