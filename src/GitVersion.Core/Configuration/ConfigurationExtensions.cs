using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

public static class ConfigurationExtensions
{
    public static BranchConfiguration GetBranchConfiguration(this GitVersionConfiguration configuration, IBranch branch)
        => GetBranchConfiguration(configuration, branch.NotNull().Name.WithoutRemote);

    public static BranchConfiguration GetBranchConfiguration(this GitVersionConfiguration configuration, string branchName)
    {
        var branchConfiguration = GetBranchConfigurations(configuration, branchName).FirstOrDefault();
        branchConfiguration ??= new()
        {
            Name = branchName,
            Regex = string.Empty,
            Label = "{BranchName}",
            Increment = IncrementStrategy.Inherit
        };

        if (branchConfiguration.Increment == IncrementStrategy.Inherit)
            return branchConfiguration;

        var fallbackBranchConfiguration = GetFallbackBranchConfiguration(configuration);
        branchConfiguration.Increment ??= fallbackBranchConfiguration.Increment;
        if (branchConfiguration.Increment != IncrementStrategy.Inherit)
        {
            branchConfiguration = branchConfiguration.Inherit(fallbackBranchConfiguration);
        }
        return branchConfiguration;
    }

    private static IEnumerable<BranchConfiguration> GetBranchConfigurations(GitVersionConfiguration configuration, string branchName)
    {
        BranchConfiguration? unknownBranchConfiguration = null;
        foreach (var item in configuration.Branches.Values.Where(b => b.Regex != null))
        {
            if (item.Regex != null && Regex.IsMatch(branchName, item.Regex, RegexOptions.IgnoreCase))
            {
                if (item.Name == "unknown")
                {
                    unknownBranchConfiguration = item;
                }
                else
                {
                    yield return item;
                }
            }
        }
        if (unknownBranchConfiguration != null) yield return unknownBranchConfiguration;
    }

    public static BranchConfiguration GetFallbackBranchConfiguration(this GitVersionConfiguration configuration)
    {
        BranchConfiguration result = new(configuration);
        result.Name ??= "fallback";
        result.Regex ??= "";
        result.Label ??= "{BranchName}";
        result.VersioningMode ??= VersioningMode.ContinuousDelivery;
        result.PreventIncrementOfMergedBranchVersion ??= false;
        result.TrackMergeTarget ??= false;
        result.TracksReleaseBranches ??= false;
        result.IsReleaseBranch ??= false;
        result.IsMainline ??= false;
        result.CommitMessageIncrementing ??= CommitMessageIncrementMode.Enabled;
        if (result.Increment == IncrementStrategy.Inherit)
        {
            result.Increment = IncrementStrategy.None;
        }
        return result;
    }

    private static BranchConfiguration? ForBranch(GitVersionConfiguration configuration, string branchName)
    {
        var matches = configuration.Branches
            .Where(b => b.Value.Regex != null && Regex.IsMatch(branchName, b.Value.Regex, RegexOptions.IgnoreCase))
            .ToArray();

        return matches.Select(kvp => kvp.Value).FirstOrDefault();
    }

    public static bool IsReleaseBranch(this GitVersionConfiguration configuration, string branchName) => configuration.GetBranchConfiguration(branchName).IsReleaseBranch ?? false;

    public static string GetBranchSpecificTag(this EffectiveConfiguration configuration, ILog log, string? branchFriendlyName, string? branchNameOverride)
    {
        var tagToUse = configuration.Label;
        if (tagToUse == "useBranchName")
        {
            tagToUse = "{BranchName}";
        }
        if (tagToUse.Contains("{BranchName}"))
        {
            log.Info("Using branch name to calculate version tag");

            var branchName = branchNameOverride ?? branchFriendlyName;
            if (!configuration.BranchPrefixToTrim.IsNullOrWhiteSpace())
            {
                var branchNameTrimmed = branchName?.RegexReplace(configuration.BranchPrefixToTrim, string.Empty, RegexOptions.IgnoreCase);
                branchName = branchNameTrimmed.IsNullOrEmpty() ? branchName : branchNameTrimmed;
            }
            branchName = branchName?.RegexReplace("[^a-zA-Z0-9-]", "-");

            tagToUse = tagToUse.Replace("{BranchName}", branchName);
        }
        return tagToUse;
    }

    public static List<KeyValuePair<string, BranchConfiguration>> GetReleaseBranchConfiguration(this GitVersionConfiguration configuration) =>
        configuration.Branches
            .Where(b => b.Value.IsReleaseBranch == true)
            .ToList();
}
