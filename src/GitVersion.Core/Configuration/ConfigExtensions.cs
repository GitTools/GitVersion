using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration;

public static class ConfigExtensions
{
    public static BranchConfig GetBranchConfiguration(this Config configuration, string branchName)
    {
        var branchConfiguration = GetConfigForBranch(configuration, branchName);
        return branchConfiguration ?? GetFallbackBranchConfiguration(configuration);
    }

    // TODO: Please make the fallback configuration also configurable in the yaml.
    public static BranchConfig GetFallbackBranchConfiguration(this Config configuration)
        => new()
        {
            Name = "Fallback",
            Regex = "",
            Tag = "{BranchName}",
            VersioningMode = configuration.VersioningMode,
            Increment = configuration.Increment ?? IncrementStrategy.Inherit
        };

    public static BranchConfig? GetConfigForBranch(this Config config, string? branchName)
    {
        if (branchName == null)
        {
            throw new ArgumentNullException(nameof(branchName));
        }

        var matches = config.Branches
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

    public static bool IsReleaseBranch(this Config config, string branchName) => config.GetConfigForBranch(branchName)?.IsReleaseBranch ?? false;

    public static string GetBranchSpecificTag(this EffectiveConfiguration configuration, ILog log, string? branchFriendlyName, string? branchNameOverride)
    {
        var tagToUse = configuration.Tag ?? "{BranchName}";
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

    public static List<KeyValuePair<string, BranchConfig>> GetReleaseBranchConfig(this Config configuration) =>
        configuration.Branches
            .Where(b => b.Value.IsReleaseBranch == true)
            .ToList();
}
