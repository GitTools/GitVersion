using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Logging;
using LibGit2Sharp;
using GitVersion.Extensions;

namespace GitVersion.Configuration
{
    public class BranchConfigurationCalculator : IBranchConfigurationCalculator
    {
        public static string FallbackConfigName = "Fallback";
        private readonly GitVersionContext context;
        private readonly ILog log;

        public BranchConfigurationCalculator(ILog log, GitVersionContext context)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.context = context;
        }

        /// <summary>
        /// Gets the <see cref="BranchConfig"/> for the current commit.
        /// </summary>
        public BranchConfig GetBranchConfiguration(Branch targetBranch, IList<Branch> excludedInheritBranches = null)
        {
            var matchingBranches = context.FullConfiguration.GetConfigForBranch(targetBranch.NameWithoutRemote());
            
            if (matchingBranches == null)
            {
                log.Info($"No branch configuration found for branch {targetBranch.FriendlyName}, falling back to default configuration");

                matchingBranches = new BranchConfig { Name = FallbackConfigName };
                context.FullConfiguration.ApplyBranchDefaults(matchingBranches, "", new List<string>());
            }

            if (matchingBranches.Increment == IncrementStrategy.Inherit)
            {
                matchingBranches = InheritBranchConfiguration(targetBranch, matchingBranches, excludedInheritBranches);
                if (matchingBranches.Name == FallbackConfigName && matchingBranches.Increment == IncrementStrategy.Inherit)
                {
                    // We tried, and failed to inherit, just fall back to patch
                    matchingBranches.Increment = IncrementStrategy.Patch;
                }
            }

            return matchingBranches;
        }

        // TODO I think we need to take a fresh approach to this.. it's getting really complex with heaps of edge cases
        private BranchConfig InheritBranchConfiguration(Branch targetBranch, BranchConfig branchConfiguration, IList<Branch> excludedInheritBranches)
        {
            var repository = context.Repository;
            var config = context.FullConfiguration;
            using (log.IndentLog("Attempting to inherit branch configuration from parent branch"))
            {
                var excludedBranches = new[] { targetBranch };
                // Check if we are a merge commit. If so likely we are a pull request
                var parentCount = context.CurrentCommit.Parents.Count();
                if (parentCount == 2)
                {
                    excludedBranches = CalculateWhenMultipleParents(repository, context.CurrentCommit, ref targetBranch, excludedBranches);
                }

                if (excludedInheritBranches == null)
                {
                    excludedInheritBranches = repository.Branches.Where(b =>
                    {
                        var branchConfig = config.GetConfigForBranch(b.NameWithoutRemote());

                        return branchConfig == null || branchConfig.Increment == IncrementStrategy.Inherit;
                    }).ToList();
                }
                // Add new excluded branches.
                foreach (var excludedBranch in excludedBranches.ExcludingBranches(excludedInheritBranches))
                {
                    excludedInheritBranches.Add(excludedBranch);
                }
                var branchesToEvaluate = repository.Branches.ExcludingBranches(excludedInheritBranches).ToList();

                var branchPoint = context.RepositoryMetadataProvider
                    .FindCommitBranchWasBranchedFrom(targetBranch, excludedInheritBranches.ToArray());
                List<Branch> possibleParents;
                if (branchPoint == BranchCommit.Empty)
                {
                    possibleParents = context.RepositoryMetadataProvider.GetBranchesContainingCommit(targetBranch.Tip, branchesToEvaluate, true)
                        // It fails to inherit Increment branch configuration if more than 1 parent;
                        // therefore no point to get more than 2 parents
                        .Take(2)
                        .ToList();
                }
                else
                {
                    var branches = context.RepositoryMetadataProvider
                        .GetBranchesContainingCommit(branchPoint.Commit, branchesToEvaluate, true).ToList();
                    if (branches.Count > 1)
                    {
                        var currentTipBranches = context.RepositoryMetadataProvider
                            .GetBranchesContainingCommit(context.CurrentCommit, branchesToEvaluate, true).ToList();
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
                    var branchConfig = GetBranchConfiguration(possibleParents[0], excludedInheritBranches);
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
                string errorMessage;
                if (possibleParents.Count == 0)
                    errorMessage = "Failed to inherit Increment branch configuration, no branches found.";
                else
                    errorMessage = "Failed to inherit Increment branch configuration, ended up with: " + string.Join(", ", possibleParents.Select(p => p.FriendlyName));

                var developBranchRegex = config.Branches[Config.DevelopBranchKey].Regex;
                var masterBranchRegex = config.Branches[Config.MasterBranchKey].Regex;

                var chosenBranch = repository.Branches.FirstOrDefault(b => Regex.IsMatch(b.FriendlyName, developBranchRegex, RegexOptions.IgnoreCase)
                                                                           || Regex.IsMatch(b.FriendlyName, masterBranchRegex, RegexOptions.IgnoreCase));
                if (chosenBranch == null)
                {
                    // TODO We should call the build server to generate this exception, each build server works differently
                    // for fetch issues and we could give better warnings.
                    throw new InvalidOperationException("Could not find a 'develop' or 'master' branch, neither locally nor remotely.");
                }

                var branchName = chosenBranch.FriendlyName;
                log.Warning(errorMessage + System.Environment.NewLine + System.Environment.NewLine + "Falling back to " + branchName + " branch config");

                // To prevent infinite loops, make sure that a new branch was chosen.
                if (targetBranch.IsSameBranch(chosenBranch))
                {
                    var developOrMasterConfig =
                        ChooseMasterOrDevelopIncrementStrategyIfTheChosenBranchIsOneOfThem(
                            chosenBranch, branchConfiguration, config);
                    if (developOrMasterConfig != null)
                    {
                        return developOrMasterConfig;
                    }
                    else
                    {
                        log.Warning("Fallback branch wants to inherit Increment branch configuration from itself. Using patch increment instead.");
                        return new BranchConfig(branchConfiguration)
                        {
                            Increment = IncrementStrategy.Patch
                        };
                    }
                }

                var inheritingBranchConfig = GetBranchConfiguration(chosenBranch, excludedInheritBranches);
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

        private Branch[] CalculateWhenMultipleParents(IRepository repository, Commit currentCommit, ref Branch currentBranch, Branch[] excludedBranches)
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
                currentBranch = branches.FirstOrDefault(b => b.NameWithoutRemote() == "master") ?? branches.First();
            }
            else
            {
                var possibleTargetBranches = repository.Branches.Where(b => !b.IsRemote && b.Tip == parents[0]).ToList();
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
