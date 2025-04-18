using System.IO.Abstractions;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal static class ConfigurationExtensions
{
    public static EffectiveBranchConfiguration GetEffectiveBranchConfiguration(
        this IGitVersionConfiguration configuration, IBranch branch, EffectiveConfiguration? parentConfiguration = null)
    {
        var effectiveConfiguration = GetEffectiveConfiguration(configuration, branch.Name, parentConfiguration);
        return new EffectiveBranchConfiguration(effectiveConfiguration, branch);
    }

    public static EffectiveConfiguration GetEffectiveConfiguration(
        this IGitVersionConfiguration configuration, ReferenceName branchName, EffectiveConfiguration? parentConfiguration = null)
    {
        var branchConfiguration = configuration.GetBranchConfiguration(branchName);
        EffectiveConfiguration? fallbackConfiguration = null;
        if (branchConfiguration.Increment == IncrementStrategy.Inherit)
        {
            fallbackConfiguration = parentConfiguration;
        }
        return new EffectiveConfiguration(configuration, branchConfiguration, fallbackConfiguration);
    }

    public static IBranchConfiguration GetBranchConfiguration(this IGitVersionConfiguration configuration, IBranch branch)
        => GetBranchConfiguration(configuration, branch.NotNull().Name);

    public static IBranchConfiguration GetBranchConfiguration(this IGitVersionConfiguration configuration, ReferenceName branchName)
    {
        var branchConfiguration = GetBranchConfigurations(configuration, branchName.WithoutOrigin).FirstOrDefault();
        branchConfiguration ??= configuration.GetEmptyBranchConfiguration();
        return branchConfiguration;
    }

    public static IEnumerable<IVersionFilter> ToFilters(this IIgnoreConfiguration source)
    {
        source.NotNull();

        if (source.Shas.Count != 0) yield return new ShaVersionFilter(source.Shas);
        if (source.Before.HasValue) yield return new MinDateVersionFilter(source.Before.Value);
    }

    private static IEnumerable<IBranchConfiguration> GetBranchConfigurations(IGitVersionConfiguration configuration, string branchName)
    {
        IBranchConfiguration? unknownBranchConfiguration = null;
        foreach ((var key, var branchConfiguration) in configuration.Branches)
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
            var regex = RegexPatterns.Cache.GetOrAdd(configuration.RegularExpression);
            var match = regex.Match(effectiveBranchName);
            if (match.Success)
            {
                foreach (var groupName in regex.GetGroupNames())
                {
                    var groupValue = match.Groups[groupName].Value;
                    Lazy<string> escapedGroupValueLazy = new(() => EscapeInvalidCharacters(groupValue));
                    var placeholder = $"{{{groupName}}}";
                    int index, startIndex = 0;
                    while ((index = label.IndexOf(placeholder, startIndex, StringComparison.InvariantCulture)) >= 0)
                    {
                        var escapedGroupValue = escapedGroupValueLazy.Value;
                        label = label.Remove(index, placeholder.Length).Insert(index, escapedGroupValue);
                        startIndex = index + escapedGroupValue.Length;
                    }
                }
            }
        }
        return label;
    }

    private static string EscapeInvalidCharacters(string groupValue) => groupValue.RegexReplace("[^a-zA-Z0-9-]", "-");

    public static (string GitDirectory, string WorkingTreeDirectory)? FindGitDir(this IFileSystem fileSystem, string? path)
    {
        var startingDir = path;
        while (startingDir is not null)
        {
            var dirOrFilePath = FileSystemHelper.Path.Combine(startingDir, ".git");
            if (fileSystem.Directory.Exists(dirOrFilePath))
            {
                return (dirOrFilePath, FileSystemHelper.Path.GetDirectoryName(dirOrFilePath));
            }

            if (fileSystem.File.Exists(dirOrFilePath))
            {
                string? relativeGitDirPath = ReadGitDirFromFile(fileSystem, dirOrFilePath);
                if (!string.IsNullOrWhiteSpace(relativeGitDirPath))
                {
                    var fullGitDirPath = FileSystemHelper.Path.GetFullPath(FileSystemHelper.Path.Combine(startingDir, relativeGitDirPath));
                    if (fileSystem.Directory.Exists(fullGitDirPath))
                    {
                        return (fullGitDirPath, FileSystemHelper.Path.GetDirectoryName(dirOrFilePath));
                    }
                }
            }

            startingDir = FileSystemHelper.Path.GetDirectoryName(startingDir);
        }

        return null;
    }

    private static string? ReadGitDirFromFile(IFileSystem fileSystem, string fileName)
    {
        const string expectedPrefix = "gitdir: ";
        var firstLineOfFile = fileSystem.File.ReadLines(fileName).FirstOrDefault();
        if (firstLineOfFile?.StartsWith(expectedPrefix) ?? false)
        {
            return firstLineOfFile[expectedPrefix.Length..]; // strip off the prefix, leaving just the path
        }

        return null;
    }
}
