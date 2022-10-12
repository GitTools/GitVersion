using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration;

public static class ConfigExtensions
{
    public static BranchConfiguration GetBranchConfiguration(this GitVersionConfiguration configuration, IBranch branch)
        => GetBranchConfiguration(configuration, branch.NotNull().Name.WithoutRemote);

    public static BranchConfiguration GetBranchConfiguration(this GitVersionConfiguration configuration, string branchName)
    {
        var branchConfiguration = ForBranch(configuration, branchName);
        if (branchConfiguration is null)
        {
            branchConfiguration = GetUnknownBranchConfiguration(configuration);
            branchConfiguration.Name = branchName;
        }
        return branchConfiguration;
    }

    // TODO: Please make the unknown settings also configurable in the yaml.
    public static BranchConfiguration GetUnknownBranchConfiguration(this GitVersionConfiguration configuration) => new()
    {
        Name = "Unknown",
        Regex = "",
        Tag = "{BranchName}",
        VersioningMode = configuration.VersioningMode,
        Increment = IncrementStrategy.Inherit
    };

    // TODO: Please make the fallback settings also configurable in the yaml.
    public static BranchConfiguration GetFallbackBranchConfiguration(this GitVersionConfiguration configuration)
    {
        var result = new BranchConfiguration()
        {
            Name = "Fallback",
            Regex = "",
            Tag = "{BranchName}",
            VersioningMode = configuration.VersioningMode,
            Increment = configuration.Increment,
            PreventIncrementOfMergedBranchVersion = false,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsReleaseBranch = false,
            IsMainline = false
        };
        if (result.Increment == IncrementStrategy.Inherit)
        {
            result.Increment = IncrementStrategy.None;
        }
        return result;
    }

    private static BranchConfiguration? ForBranch(Model.Configuration.GitVersionConfiguration configuration, string branchName)
    {
        var matches = configuration.Branches
            .Where(b => b.Value?.Regex != null && Regex.IsMatch(branchName, b.Value.Regex, RegexOptions.IgnoreCase))
            .ToArray();

        try
        {
            return matches
                .Select(kvp => kvp.Value)
                .SingleOrDefault();
        }
        catch (InvalidOperationException)
        {
            var matchingConfigs = string.Concat(matches.Select(m => $"{System.Environment.NewLine} - {m.Key}"));
            var picked = matches
                .Select(kvp => kvp.Value)
                .First();

            // TODO check how to log this
            Console.WriteLine(
                $"Multiple branch configurations match the current branch branchName of '{branchName}'. " +
                $"Using the first matching configuration, '{picked?.Name}'. Matching configurations include:'{matchingConfigs}'");

            return picked;
        }
    }

    public static bool IsReleaseBranch(this GitVersionConfiguration configuration, string branchName) => configuration.GetBranchConfiguration(branchName).IsReleaseBranch ?? false;

    public static string GetBranchSpecificTag(this EffectiveConfiguration configuration, ILog log, string? branchFriendlyName, string? branchNameOverride)
    {
        var tagToUse = configuration.Tag;
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
