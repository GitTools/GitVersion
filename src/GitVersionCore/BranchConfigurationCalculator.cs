namespace GitVersion
{
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
        public static BranchConfig GetBranchConfiguration(GitVersionContext context, Branch targetBranch, IList<Branch> excludedInheritBranches = null)
        {
            var matchingBranches = context.FullConfiguration.GetConfigForBranch(targetBranch.FriendlyName);
            
            if (matchingBranches == null)
            {
                Logger.WriteInfo(string.Format(
                    "No branch configuration found for branch {0}, falling back to default configuration",
                    targetBranch.FriendlyName));

                matchingBranches = new BranchConfig { Name = string.Empty };
                ConfigurationProvider.ApplyBranchDefaults(context.FullConfiguration, matchingBranches, "", new List<string>());
            }

            return matchingBranches.Increment == IncrementStrategy.Inherit ?
                InheritBranchConfiguration(context, targetBranch, matchingBranches, excludedInheritBranches) :
                matchingBranches;
        }

        static BranchConfig InheritBranchConfiguration(GitVersionContext context, Branch targetBranch, BranchConfig branchConfiguration, IList<Branch> excludedInheritBranches)
        {
            var repository = context.Repository;
            var config = context.FullConfiguration;
            using (Logger.IndentLog("Attempting to inherit branch configuration from parent branch"))
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
                        var branchConfig = config.GetConfigForBranch(b.FriendlyName);

                        return branchConfig != null && branchConfig.Increment == IncrementStrategy.Inherit;
                    }).ToList();
                }
                // Add new excluded branches.
                foreach (var excludedBranch in excludedBranches.ExcludingBranches(excludedInheritBranches))
                {
                    excludedInheritBranches.Add(excludedBranch);
                }
                var branchesToEvaluate = repository.Branches.Except(excludedInheritBranches).ToList();

                var branchPoint = context.RepositoryMetadataProvider
                    .FindCommitBranchWasBranchedFrom(targetBranch, excludedInheritBranches.ToArray());
                List<Branch> possibleParents;
                if (branchPoint == BranchCommit.Empty)
                {
                    possibleParents = context.RepositoryMetadataProvider.GetBranchesContainingCommit(context.CurrentCommit, branchesToEvaluate, true)
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

                Logger.WriteInfo("Found possible parent branches: " + string.Join(", ", possibleParents.Select(p => p.FriendlyName)));

                if (possibleParents.Count == 1)
                {
                    var branchConfig = GetBranchConfiguration(context, possibleParents[0], excludedInheritBranches);
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

                var developBranchRegex = config.Branches[ConfigurationProvider.DevelopBranchKey].Regex;
                var masterBranchRegex = config.Branches[ConfigurationProvider.MasterBranchKey].Regex;

                var chosenBranch = repository.Branches.FirstOrDefault(b => Regex.IsMatch(b.FriendlyName, developBranchRegex, RegexOptions.IgnoreCase)
                                                                           || Regex.IsMatch(b.FriendlyName, masterBranchRegex, RegexOptions.IgnoreCase));
                if (chosenBranch == null)
                {
                    // TODO We should call the build server to generate this exception, each build server works differently
                    // for fetch issues and we could give better warnings.
                    throw new InvalidOperationException("Could not find a 'develop' or 'master' branch, neither locally nor remotely.");
                }

                var branchName = chosenBranch.FriendlyName;
                Logger.WriteWarning(errorMessage + Environment.NewLine + Environment.NewLine + "Falling back to " + branchName + " branch config");

                // To prevent infinite loops, make sure that a new branch was chosen.
                if (targetBranch.IsSameBranch(chosenBranch))
                {
                    Logger.WriteWarning("Fallback branch wants to inherit Increment branch configuration from itself. Using patch increment instead.");
                    return new BranchConfig(branchConfiguration)
                    {
                        Increment = IncrementStrategy.Patch
                    };
                }

                var inheritingBranchConfig = GetBranchConfiguration(context, chosenBranch, excludedInheritBranches);
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