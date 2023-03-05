using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Configuration;

public static class ConfigurationExtensions
{
    public static EffectiveConfiguration GetEffectiveConfiguration(this GitVersionConfiguration configuration, IBranch branch)
        => GetEffectiveConfiguration(configuration, branch.NotNull().Name);

    private static EffectiveConfiguration GetEffectiveConfiguration(this GitVersionConfiguration configuration, ReferenceName branchName)
    {
        BranchConfiguration branchConfiguration = configuration.GetBranchConfiguration(branchName);
        return new EffectiveConfiguration(configuration, branchConfiguration);
    }

    public static BranchConfiguration GetBranchConfiguration(this GitVersionConfiguration configuration, IBranch branch)
        => GetBranchConfiguration(configuration, branch.NotNull().Name);

    public static BranchConfiguration GetBranchConfiguration(this GitVersionConfiguration configuration, ReferenceName branchName)
    {
        var branchConfiguration = GetBranchConfigurations(configuration, branchName.WithoutRemote).FirstOrDefault();
        branchConfiguration ??= new() { Name = branchName.WithoutRemote, Regex = string.Empty, Label = ConfigurationConstants.BranchNamePlaceholder, Increment = IncrementStrategy.Inherit };
        return branchConfiguration;
    }

    private static IEnumerable<BranchConfiguration> GetBranchConfigurations(GitVersionConfiguration configuration, string branchName)
    {
        BranchConfiguration? unknownBranchConfiguration = null;
        foreach (var item in configuration.Branches.Values.Where(branch => branch.Regex != null))
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
        return result;
    }

    public static bool IsReleaseBranch(this GitVersionConfiguration configuration, IBranch branch)
        => IsReleaseBranch(configuration, branch.NotNull().Name);

    public static bool IsReleaseBranch(this GitVersionConfiguration configuration, ReferenceName branchName)
        => configuration.GetBranchConfiguration(branchName).IsReleaseBranch ?? false;

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

    public static (string GitDirectory, string WorkingTreeDirectory)? FindGitDir(this string path)
    {
        string? startingDir = path;
        while (startingDir is not null)
        {
            var dirOrFilePath = Path.Combine(startingDir, ".git");
            if (Directory.Exists(dirOrFilePath))
            {
                return (dirOrFilePath, Path.GetDirectoryName(dirOrFilePath)!);
            }

            if (File.Exists(dirOrFilePath))
            {
                string? relativeGitDirPath = ReadGitDirFromFile(dirOrFilePath);
                if (!string.IsNullOrWhiteSpace(relativeGitDirPath))
                {
                    var fullGitDirPath = Path.GetFullPath(Path.Combine(startingDir, relativeGitDirPath));
                    if (Directory.Exists(fullGitDirPath))
                    {
                        return (fullGitDirPath, Path.GetDirectoryName(dirOrFilePath)!);
                    }
                }
            }

            startingDir = Path.GetDirectoryName(startingDir);
        }

        return null;
    }

    private static string? ReadGitDirFromFile(string fileName)
    {
        const string expectedPrefix = "gitdir: ";
        var firstLineOfFile = File.ReadLines(fileName).FirstOrDefault();
        if (firstLineOfFile?.StartsWith(expectedPrefix) ?? false)
        {
            return firstLineOfFile[expectedPrefix.Length..]; // strip off the prefix, leaving just the path
        }

        return null;
    }

    public static List<KeyValuePair<string, BranchConfiguration>> GetReleaseBranchConfiguration(this GitVersionConfiguration configuration) =>
        configuration.Branches
            .Where(b => b.Value.IsReleaseBranch == true)
            .ToList();
}
