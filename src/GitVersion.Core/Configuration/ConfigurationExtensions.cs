using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Configuration;

public static class ConfigurationExtensions
{
    public static EffectiveConfiguration GetEffectiveConfiguration(this GitVersionConfiguration configuration, IBranch branch)
        => GetEffectiveConfiguration(configuration, branch.NotNull().Name.WithoutRemote);

    public static EffectiveConfiguration GetEffectiveConfiguration(this GitVersionConfiguration configuration, string branchName)
    {
        BranchConfiguration branchConfiguration = configuration.GetBranchConfiguration(branchName);
        return new EffectiveConfiguration(configuration, branchConfiguration);
    }

    public static BranchConfiguration GetBranchConfiguration(this GitVersionConfiguration configuration, IBranch branch)
        => GetBranchConfiguration(configuration, branch.NotNull().Name.WithoutRemote);

    public static BranchConfiguration GetBranchConfiguration(this GitVersionConfiguration configuration, string branchName)
    {
        var branchConfiguration = GetBranchConfigurations(configuration, branchName).FirstOrDefault();
        branchConfiguration ??= new()
        {
            Name = branchName,
            Regex = string.Empty,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            Increment = IncrementStrategy.Inherit
        };
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
        if (result.Increment == IncrementStrategy.Inherit) result.Increment = IncrementStrategy.None;
        return result;
    }

    public static bool IsReleaseBranch(this GitVersionConfiguration configuration, string branchName) => configuration.GetBranchConfiguration(branchName).IsReleaseBranch ?? false;

    public static string GetBranchSpecificTag(this EffectiveConfiguration configuration, ILog log, string? branchFriendlyName, string? branchNameOverride)
    {
        var tagToUse = configuration.Label;
        if (tagToUse == "useBranchName")
        {
            tagToUse = ConfigurationConstants.BranchNamePlaceholder;
        }
        if (tagToUse.Contains(ConfigurationConstants.BranchNamePlaceholder))
        {
            log.Info("Using branch name to calculate version tag");

            var branchName = branchNameOverride ?? branchFriendlyName;
            if (!configuration.BranchPrefixToTrim.IsNullOrWhiteSpace())
            {
                var branchNameTrimmed = branchName?.RegexReplace(configuration.BranchPrefixToTrim, string.Empty, RegexOptions.IgnoreCase);
                branchName = branchNameTrimmed.IsNullOrEmpty() ? branchName : branchNameTrimmed;
            }
            branchName = branchName?.RegexReplace("[^a-zA-Z0-9-]", "-");

            tagToUse = tagToUse.Replace(ConfigurationConstants.BranchNamePlaceholder, branchName);
        }
        return tagToUse;
    }

    public static List<KeyValuePair<string, BranchConfiguration>> GetReleaseBranchConfiguration(this GitVersionConfiguration configuration) =>
        configuration.Branches
            .Where(b => b.Value.IsReleaseBranch == true)
            .ToList();
}
