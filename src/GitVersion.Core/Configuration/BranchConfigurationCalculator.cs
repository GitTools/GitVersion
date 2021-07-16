using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Common;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration
{
    public class BranchConfigurationCalculator : IBranchConfigurationCalculator
    {
        private const string FallbackConfigName = "Fallback";

        private readonly ILog log;
        private readonly IRepositoryStore repositoryStore;

        public BranchConfigurationCalculator(ILog log, IRepositoryStore repositoryStore)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.repositoryStore = repositoryStore ?? throw new ArgumentNullException(nameof(repositoryStore));
        }

        /// <summary>
        /// Gets the <see cref="BranchConfig"/> for the current commit.
        /// </summary>
        public BranchConfig GetBranchConfiguration(IBranch targetBranch, ICommit currentCommit, Config configuration, IList<IBranch> excludedInheritBranches = null)
        {
            var matchingBranches = configuration.GetConfigForBranch(targetBranch.Name.WithoutRemote);

            if (matchingBranches == null)
            {
                log.Info($"No branch configuration found for branch {targetBranch}, falling back to default configuration");

                matchingBranches = BranchConfig.CreateDefaultBranchConfig(FallbackConfigName)
                                               .Apply(new BranchConfig
                                               {
                                                   Regex = "",
                                                   VersioningMode = configuration.VersioningMode,
                                                   Increment = configuration.Increment ?? IncrementStrategy.Inherit,
                                               });
            }

            if (matchingBranches.Increment == IncrementStrategy.Inherit)
            {
                matchingBranches = InheritBranchConfiguration(targetBranch, matchingBranches, currentCommit, configuration, excludedInheritBranches);
                if (matchingBranches.Name.IsEquivalentTo(FallbackConfigName) && matchingBranches.Increment == IncrementStrategy.Inherit)
                {
                    // We tried, and failed to inherit, just fall back to patch
                    matchingBranches.Increment = IncrementStrategy.Patch;
                }
            }

            return matchingBranches;
        }

        // TODO I think we need to take a fresh approach to this.. it's getting really complex with heaps of edge cases
        private BranchConfig InheritBranchConfiguration(IBranch targetBranch, BranchConfig branchConfiguration, ICommit currentCommit, Config configuration, IList<IBranch> excludedInheritBranches)
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

                excludedInheritBranches ??= repositoryStore.GetExcludedInheritBranches(configuration).ToList();

                excludedBranches = excludedBranches.Where(b => excludedInheritBranches.All(bte => !b.Equals(bte))).ToArray();
                // Add new excluded branches.
                foreach (var excludedBranch in excludedBranches)
                {
                    excludedInheritBranches.Add(excludedBranch);
                }
                var branchesToEvaluate = repositoryStore.ExcludingBranches(excludedInheritBranches)
                    .Distinct(new LocalRemoteBranchEqualityComparer())
                    .ToList();

                var branchPoint = repositoryStore
                    .FindCommitBranchWasBranchedFrom(targetBranch, configuration, excludedInheritBranches.ToArray());
                List<IBranch> possibleParents;
                if (branchPoint == BranchCommit.Empty)
                {
                    possibleParents = repositoryStore.GetBranchesContainingCommit(targetBranch.Tip, branchesToEvaluate)
                        // It fails to inherit Increment branch configuration if more than 1 parent;
                        // therefore no point to get more than 2 parents
                        .Take(2)
                        .ToList();
                }
                else
                {
                    var branches = repositoryStore.GetBranchesContainingCommit(branchPoint.Commit, branchesToEvaluate).ToList();
                    if (branches.Count > 1)
                    {
                        var currentTipBranches = repositoryStore.GetBranchesContainingCommit(currentCommit, branchesToEvaluate).ToList();
                        possibleParents = branches.Except(currentTipBranches).ToList();
                    }
                    else
                    {
                        possibleParents = branches;
                    }
                }

                log.Info("Found possible parent branches: " + string.Join(", ", possibleParents.Select(p => p.ToString())));

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
                // if develop exists and main if not
                var errorMessage = possibleParents.Count == 0
                    ? "Failed to inherit Increment branch configuration, no branches found."
                    : "Failed to inherit Increment branch configuration, ended up with: " + string.Join(", ", possibleParents.Select(p => p.ToString()));

                var chosenBranch = repositoryStore.GetChosenBranch(configuration);
                if (chosenBranch == null)
                {
                    // TODO We should call the build server to generate this exception, each build server works differently
                    // for fetch issues and we could give better warnings.
                    throw new InvalidOperationException("Gitversion could not determine which branch to treat as the development branch (default is 'develop') nor releaseable branch (default is 'main' or 'master'), either locally or remotely. Ensure the local clone and checkout match the requirements or considering using 'GitVersion Dynamic Repositories'");
                }

                log.Warning($"{errorMessage}{System.Environment.NewLine}Falling back to {chosenBranch} branch config");

                // To prevent infinite loops, make sure that a new branch was chosen.
                if (targetBranch.Equals(chosenBranch))
                {
                    var developOrMainConfig =
                        ChooseMainOrDevelopIncrementStrategyIfTheChosenBranchIsOneOfThem(
                            chosenBranch, branchConfiguration, configuration);
                    if (developOrMainConfig != null)
                    {
                        return developOrMainConfig;
                    }

                    log.Warning("Fallback branch wants to inherit Increment branch configuration from itself. Using patch increment instead.");
                    return new BranchConfig(branchConfiguration)
                    {
                        Increment = IncrementStrategy.Patch
                    };
                }

                var inheritingBranchConfig = GetBranchConfiguration(chosenBranch, currentCommit, configuration, excludedInheritBranches);
                var configIncrement = inheritingBranchConfig.Increment;
                if (inheritingBranchConfig.Name.IsEquivalentTo(FallbackConfigName) && configIncrement == IncrementStrategy.Inherit)
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

        private IBranch[] CalculateWhenMultipleParents(ICommit currentCommit, ref IBranch currentBranch, IBranch[] excludedBranches)
        {
            var parents = currentCommit.Parents.ToArray();
            var branches = repositoryStore.GetBranchesForCommit(parents[1]).ToList();
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
                currentBranch = branches.FirstOrDefault(b => b.Name.WithoutRemote == Config.MainBranchKey) ?? branches.First();
            }
            else
            {
                var possibleTargetBranches = repositoryStore.GetBranchesForCommit(parents[0]).ToList();
                if (possibleTargetBranches.Count > 1)
                {
                    currentBranch = possibleTargetBranches.FirstOrDefault(b => b.Name.WithoutRemote == Config.MainBranchKey) ?? possibleTargetBranches.First();
                }
                else
                {
                    currentBranch = possibleTargetBranches.FirstOrDefault() ?? currentBranch;
                }
            }

            log.Info($"HEAD is merge commit, this is likely a pull request using {currentBranch} as base");

            return excludedBranches;
        }



        private static BranchConfig ChooseMainOrDevelopIncrementStrategyIfTheChosenBranchIsOneOfThem(IBranch chosenBranch, BranchConfig branchConfiguration, Config config)
        {
            BranchConfig mainOrDevelopConfig = null;
            var developBranchRegex = config.Branches[Config.DevelopBranchKey].Regex;
            var mainBranchRegex = config.Branches[Config.MainBranchKey].Regex;
            if (Regex.IsMatch(chosenBranch.Name.Friendly, developBranchRegex, RegexOptions.IgnoreCase))
            {
                // Normally we would not expect this to happen but for safety we add a check
                if (config.Branches[Config.DevelopBranchKey].Increment !=
                    IncrementStrategy.Inherit)
                {
                    mainOrDevelopConfig = new BranchConfig(branchConfiguration)
                    {
                        Increment = config.Branches[Config.DevelopBranchKey].Increment
                    };
                }
            }
            else if (Regex.IsMatch(chosenBranch.Name.Friendly, mainBranchRegex, RegexOptions.IgnoreCase))
            {
                // Normally we would not expect this to happen but for safety we add a check
                if (config.Branches[Config.MainBranchKey].Increment !=
                    IncrementStrategy.Inherit)
                {
                    mainOrDevelopConfig = new BranchConfig(branchConfiguration)
                    {
                        Increment = config.Branches[Config.DevelopBranchKey].Increment
                    };
                }
            }
            return mainOrDevelopConfig;
        }

        private class LocalRemoteBranchEqualityComparer : IEqualityComparer<IBranch>
        {
            public bool Equals(IBranch b1, IBranch b2)
            {
                if (b1 == null && b2 == null)
                    return true;
                if (b1 == null || b2 == null)
                    return false;

                return b1.Name.WithoutRemote.Equals(b2.Name.WithoutRemote);
            }

            public int GetHashCode(IBranch b)
            {
                return b.Name.WithoutRemote.GetHashCode();
            }
        }
    }
}
