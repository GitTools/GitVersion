using GitVersion.Extensions;
using GitVersion.Formatting;
using GitVersion.Git;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal static class ConfigurationExtensions
{
    extension(IGitVersionConfiguration configuration)
    {
        public EffectiveBranchConfiguration GetEffectiveBranchConfiguration(IBranch branch, EffectiveConfiguration? parentConfiguration = null)
        {
            var effectiveConfiguration = GetEffectiveConfiguration(configuration, branch.Name, parentConfiguration);
            return new EffectiveBranchConfiguration(effectiveConfiguration, branch);
        }

        public EffectiveConfiguration GetEffectiveConfiguration(ReferenceName branchName, EffectiveConfiguration? parentConfiguration = null)
        {
            var branchConfiguration = configuration.GetBranchConfiguration(branchName);
            EffectiveConfiguration? fallbackConfiguration = null;
            if (branchConfiguration.Increment == IncrementStrategy.Inherit)
            {
                fallbackConfiguration = parentConfiguration;
            }
            return new EffectiveConfiguration(configuration, branchConfiguration, fallbackConfiguration);
        }

        public IBranchConfiguration GetBranchConfiguration(IBranch branch)
            => GetBranchConfiguration(configuration, branch.NotNull().Name);

        public IBranchConfiguration GetBranchConfiguration(ReferenceName branchName)
        {
            var branchConfiguration = GetBranchConfigurations(configuration, branchName.WithoutOrigin).FirstOrDefault();
            branchConfiguration ??= configuration.GetEmptyBranchConfiguration();
            return branchConfiguration;

            static IEnumerable<IBranchConfiguration> GetBranchConfigurations(IGitVersionConfiguration configuration, string branchName)
            {
                IBranchConfiguration? unknownBranchConfiguration = null;
                foreach (var (key, branchConfiguration) in configuration.Branches)
                {
                    if (!branchConfiguration.IsMatch(branchName)) continue;
                    if (key == "unknown")
                    {
                        unknownBranchConfiguration = branchConfiguration;
                    }
                    else
                    {
                        yield return branchConfiguration;
                    }
                }

                if (unknownBranchConfiguration != null) yield return unknownBranchConfiguration;
            }
        }

        public IBranchConfiguration GetFallbackBranchConfiguration() => configuration;

        public bool IsReleaseBranch(IBranch branch)
            => IsReleaseBranch(configuration, branch.NotNull().Name);

        public bool IsReleaseBranch(ReferenceName branchName)
            => configuration.GetBranchConfiguration(branchName).IsReleaseBranch ?? false;
    }

    extension(IIgnoreConfiguration ignoreConfig)
    {
        public IEnumerable<IVersionFilter> ToFilters()
        {
            ignoreConfig.NotNull();

            if (ignoreConfig.Shas.Count != 0) yield return new ShaVersionFilter(ignoreConfig.Shas);
            if (ignoreConfig.Before.HasValue) yield return new MinDateVersionFilter(ignoreConfig.Before.Value);
            if (ignoreConfig.Paths.Count != 0) yield return new PathFilter([.. ignoreConfig.Paths]);
        }

        public IEnumerable<ITag> Filter(ITag[] source)
        {
            ignoreConfig.NotNull();
            source.NotNull();

            return !ignoreConfig.IsEmpty ? source.Where(element => ShouldBeIgnored(element.Commit, ignoreConfig)) : source;
        }

        public IEnumerable<ICommit> Filter(ICommit[] source)
        {
            ignoreConfig.NotNull();
            source.NotNull();

            return !ignoreConfig.IsEmpty ? source.Where(element => ShouldBeIgnored(element, ignoreConfig)) : source;
        }
    }

    private static bool ShouldBeIgnored(ICommit commit, IIgnoreConfiguration ignore)
        => !ignore.ToFilters().Any(filter => filter.Exclude(commit, out _));

    extension(EffectiveConfiguration configuration)
    {
        public string? GetBranchSpecificLabel(ReferenceName branchName, string? branchNameOverride, IEnvironment environment)
            => GetBranchSpecificLabel(configuration, branchName.WithoutOrigin, branchNameOverride, environment);

        public string? GetBranchSpecificLabel(string? branchName, string? branchNameOverride, IEnvironment environment)
        {
            configuration.NotNull();
            environment.NotNull();

            var label = configuration.Label;

            if (label is null)
            {
                return label;
            }

            var effectiveBranchName = branchNameOverride ?? branchName;
            var labelPlaceholders = BuildLabelPlaceholders(configuration.RegularExpression, effectiveBranchName);

            return label.FormatWith(labelPlaceholders, environment);
        }

        public TaggedSemanticVersions GetTaggedSemanticVersion()
        {
            configuration.NotNull();

            var taggedSemanticVersion = TaggedSemanticVersions.OfBranch;

            if (configuration.TrackMergeTarget)
            {
                taggedSemanticVersion |= TaggedSemanticVersions.OfMergeTargets;
            }

            if (configuration.TracksReleaseBranches)
            {
                taggedSemanticVersion |= TaggedSemanticVersions.OfReleaseBranches;
            }

            if (configuration is { IsMainBranch: false, IsReleaseBranch: false })
            {
                taggedSemanticVersion |= TaggedSemanticVersions.OfMainBranches;
            }
            return taggedSemanticVersion;
        }

        private static Dictionary<string, object> BuildLabelPlaceholders(string? regularExpression, string? effectiveBranchName)
        {
            var placeholders = new Dictionary<string, object>();

            if (regularExpression.IsNullOrWhiteSpace() || effectiveBranchName.IsNullOrEmpty())
                return placeholders;

            var regex = RegexPatterns.Cache.GetOrAdd(regularExpression);
            var match = regex.Match(effectiveBranchName);

            if (!match.Success)
                return placeholders;

            foreach (var groupName in regex.GetGroupNames().Skip(1))
            {
                var groupValue = match.Groups[groupName].Value;

                placeholders[groupName] = groupValue.RegexReplace(RegexPatterns.SanitizeNameRegexPattern, "-");
            }

            return placeholders;
        }
    }
}
