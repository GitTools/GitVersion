using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration;

public static class ConfigExtensions
{
    public static BranchConfig? GetConfigForBranch(this Config config, string? branchName)
    {
        if (branchName == null) throw new ArgumentNullException(nameof(branchName));
        var matches = config.Branches
            .Where(b => Regex.IsMatch(branchName, b.Value?.Regex, RegexOptions.IgnoreCase))
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

    public static EffectiveConfiguration CalculateEffectiveConfiguration(this Config configuration, BranchConfig currentBranchConfig)
    {
        var name = currentBranchConfig.Name;
        if (!currentBranchConfig.VersioningMode.HasValue)
            throw new Exception($"Configuration value for 'Versioning mode' for branch {name} has no value. (this should not happen, please report an issue)");
        if (!currentBranchConfig.Increment.HasValue)
            throw new Exception($"Configuration value for 'Increment' for branch {name} has no value. (this should not happen, please report an issue)");
        if (!currentBranchConfig.PreventIncrementOfMergedBranchVersion.HasValue)
            throw new Exception($"Configuration value for 'PreventIncrementOfMergedBranchVersion' for branch {name} has no value. (this should not happen, please report an issue)");
        if (!currentBranchConfig.TrackMergeTarget.HasValue)
            throw new Exception($"Configuration value for 'TrackMergeTarget' for branch {name} has no value. (this should not happen, please report an issue)");
        if (!currentBranchConfig.TracksReleaseBranches.HasValue)
            throw new Exception($"Configuration value for 'TracksReleaseBranches' for branch {name} has no value. (this should not happen, please report an issue)");
        if (!currentBranchConfig.IsReleaseBranch.HasValue)
            throw new Exception($"Configuration value for 'IsReleaseBranch' for branch {name} has no value. (this should not happen, please report an issue)");

        if (!configuration.AssemblyVersioningScheme.HasValue)
            throw new Exception("Configuration value for 'AssemblyVersioningScheme' has no value. (this should not happen, please report an issue)");
        if (!configuration.AssemblyFileVersioningScheme.HasValue)
            throw new Exception("Configuration value for 'AssemblyFileVersioningScheme' has no value. (this should not happen, please report an issue)");
        if (!configuration.CommitMessageIncrementing.HasValue)
            throw new Exception("Configuration value for 'CommitMessageIncrementing' has no value. (this should not happen, please report an issue)");
        if (!configuration.LegacySemVerPadding.HasValue)
            throw new Exception("Configuration value for 'LegacySemVerPadding' has no value. (this should not happen, please report an issue)");
        if (!configuration.BuildMetaDataPadding.HasValue)
            throw new Exception("Configuration value for 'BuildMetaDataPadding' has no value. (this should not happen, please report an issue)");
        if (!configuration.CommitsSinceVersionSourcePadding.HasValue)
            throw new Exception("Configuration value for 'CommitsSinceVersionSourcePadding' has no value. (this should not happen, please report an issue)");
        if (!configuration.TagPreReleaseWeight.HasValue)
            throw new Exception("Configuration value for 'TagPreReleaseWeight' has no value. (this should not happen, please report an issue)");

        var versioningMode = currentBranchConfig.VersioningMode.Value;
        var tag = currentBranchConfig.Tag;
        var tagNumberPattern = currentBranchConfig.TagNumberPattern;
        var incrementStrategy = currentBranchConfig.Increment.Value;
        var preventIncrementForMergedBranchVersion = currentBranchConfig.PreventIncrementOfMergedBranchVersion.Value;
        var trackMergeTarget = currentBranchConfig.TrackMergeTarget.Value;
        var preReleaseWeight = currentBranchConfig.PreReleaseWeight ?? 0;

        var nextVersion = configuration.NextVersion;
        var assemblyVersioningScheme = configuration.AssemblyVersioningScheme.Value;
        var assemblyFileVersioningScheme = configuration.AssemblyFileVersioningScheme.Value;
        var assemblyInformationalFormat = configuration.AssemblyInformationalFormat;
        var assemblyVersioningFormat = configuration.AssemblyVersioningFormat;
        var assemblyFileVersioningFormat = configuration.AssemblyFileVersioningFormat;
        var gitTagPrefix = configuration.TagPrefix;
        var majorMessage = configuration.MajorVersionBumpMessage;
        var minorMessage = configuration.MinorVersionBumpMessage;
        var patchMessage = configuration.PatchVersionBumpMessage;
        var noBumpMessage = configuration.NoBumpMessage;
        var commitDateFormat = configuration.CommitDateFormat;
        var updateBuildNumber = configuration.UpdateBuildNumber ?? true;
        var tagPreReleaseWeight = configuration.TagPreReleaseWeight.Value;

        var commitMessageVersionBump = currentBranchConfig.CommitMessageIncrementing ?? configuration.CommitMessageIncrementing.Value;
        return new EffectiveConfiguration(
            assemblyVersioningScheme, assemblyFileVersioningScheme, assemblyInformationalFormat, assemblyVersioningFormat, assemblyFileVersioningFormat, versioningMode, gitTagPrefix,
            tag, nextVersion, incrementStrategy,
            currentBranchConfig.Regex,
            preventIncrementForMergedBranchVersion,
            tagNumberPattern, configuration.ContinuousDeploymentFallbackTag,
            trackMergeTarget,
            majorMessage, minorMessage, patchMessage, noBumpMessage,
            commitMessageVersionBump,
            configuration.LegacySemVerPadding.Value,
            configuration.BuildMetaDataPadding.Value,
            configuration.CommitsSinceVersionSourcePadding.Value,
            configuration.Ignore.ToFilters(),
            currentBranchConfig.TracksReleaseBranches.Value,
            currentBranchConfig.IsReleaseBranch.Value,
            commitDateFormat,
            updateBuildNumber,
            preReleaseWeight,
            tagPreReleaseWeight);
    }

    public static string GetBranchSpecificTag(this EffectiveConfiguration configuration, ILog log, string? branchFriendlyName, string? branchNameOverride)
    {
        var tagToUse = configuration.Tag!;
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

    public static List<KeyValuePair<string, BranchConfig?>> GetReleaseBranchConfig(this Config configuration) =>
        configuration.Branches
            .Where(b => b.Value?.IsReleaseBranch == true)
            .ToList();
}
