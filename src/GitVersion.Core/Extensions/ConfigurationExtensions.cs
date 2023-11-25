using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

public static class ConfigurationExtensions
{
    public static EffectiveBranchConfiguration GetEffectiveBranchConfiguration(this IGitVersionConfiguration configuration, IBranch branch)
    {
        var effectiveConfiguration = GetEffectiveConfiguration(configuration, branch.Name);
        return new(effectiveConfiguration, branch);
    }

    public static EffectiveConfiguration GetEffectiveConfiguration(this IGitVersionConfiguration configuration, ReferenceName branchName)
    {
        var branchConfiguration = configuration.GetBranchConfiguration(branchName);
        return new(configuration, branchConfiguration);
    }

    public static IBranchConfiguration GetBranchConfiguration(this IGitVersionConfiguration configuration, IBranch branch)
        => GetBranchConfiguration(configuration, branch.NotNull().Name);

    public static IBranchConfiguration GetBranchConfiguration(this IGitVersionConfiguration configuration, ReferenceName branchName)
    {
        var branchConfiguration = GetBranchConfigurations(configuration, branchName.WithoutOrigin).FirstOrDefault();
        branchConfiguration ??= configuration.Empty();
        return branchConfiguration;
    }

    public static IEnumerable<IVersionFilter> ToFilters(this IIgnoreConfiguration source)
    {
        source.NotNull();

        if (source.Shas.Any()) yield return new ShaVersionFilter(source.Shas);
        if (source.Before.HasValue) yield return new MinDateVersionFilter(source.Before.Value);
    }

    private static IEnumerable<IBranchConfiguration> GetBranchConfigurations(IGitVersionConfiguration configuration, string branchName)
    {
        IBranchConfiguration? unknownBranchConfiguration = null;
        foreach ((string key, IBranchConfiguration branchConfiguration) in configuration.Branches)
        {
            if (branchConfiguration.IsMatch(branchName))
            {
                if (key == "unknown")
                {
                    unknownBranchConfiguration = branchConfiguration;
                }
                else
                {
                    yield return branchConfiguration;
                }
            }
        }

        if (unknownBranchConfiguration != null) yield return unknownBranchConfiguration;
    }

    public static IBranchConfiguration GetFallbackBranchConfiguration(this IGitVersionConfiguration configuration) => configuration;

    public static bool IsReleaseBranch(this IGitVersionConfiguration configuration, IBranch branch)
        => IsReleaseBranch(configuration, branch.NotNull().Name);

    public static bool IsReleaseBranch(this IGitVersionConfiguration configuration, ReferenceName branchName)
        => configuration.GetBranchConfiguration(branchName).IsReleaseBranch ?? false;

    public static string? GetBranchSpecificLabel(
            this EffectiveConfiguration configuration, ReferenceName branchName, string? branchNameOverride)
        => GetBranchSpecificLabel(configuration, branchName.WithoutOrigin, branchNameOverride);

    public static string? GetBranchSpecificLabel(
        this EffectiveConfiguration configuration, string? branchName, string? branchNameOverride)
    {
        configuration.NotNull();

        var label = configuration.Label;
        if (label is null)
        {
            return label;
        }

        var effectiveBranchName = branchNameOverride ?? branchName;

        if (!configuration.RegularExpression.IsNullOrWhiteSpace() && !effectiveBranchName.IsNullOrEmpty())
        {
            effectiveBranchName = effectiveBranchName.RegexReplace("[^a-zA-Z0-9-_]", "-");
            var pattern = new Regex(configuration.RegularExpression, RegexOptions.IgnoreCase);
            var match = pattern.Match(effectiveBranchName);
            if (match.Success)
            {
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var groupName in pattern.GetGroupNames())
                {
                    label = label.Replace("{" + groupName + "}", match.Groups[groupName].Value);
                }
            }
        }

        // Evaluate tag number pattern and append to prerelease tag, preserving build metadata
        if (!configuration.LabelNumberPattern.IsNullOrEmpty() && !effectiveBranchName.IsNullOrEmpty())
        {
            var match = Regex.Match(effectiveBranchName, configuration.LabelNumberPattern);
            var numberGroup = match.Groups["number"];
            if (numberGroup.Success)
            {
                label += numberGroup.Value;
            }
        }

        return label;
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

    public static List<KeyValuePair<string, IBranchConfiguration>> GetReleaseBranchConfiguration(this IGitVersionConfiguration configuration) =>
        configuration.Branches.Where(b => b.Value.IsReleaseBranch == true).ToList();
}
