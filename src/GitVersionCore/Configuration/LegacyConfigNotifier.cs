namespace GitVersion
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    public class LegacyConfigNotifier
    {
        static readonly Dictionary<string, string> OldConfigKnownRegexes = new Dictionary<string, string>
        {
            {ConfigurationProvider.MasterBranchRegex, ConfigurationProvider.MasterBranchKey},
            {ConfigurationProvider.DevelopBranchRegex, ConfigurationProvider.DevelopBranchKey},
            {ConfigurationProvider.FeatureBranchRegex, ConfigurationProvider.FeatureBranchKey},
            {ConfigurationProvider.HotfixBranchRegex, ConfigurationProvider.HotfixBranchKey},
            {ConfigurationProvider.ReleaseBranchRegex, ConfigurationProvider.ReleaseBranchKey},
            {ConfigurationProvider.SupportBranchRegex, ConfigurationProvider.SupportBranchKey},
            {ConfigurationProvider.PullRequestRegex, ConfigurationProvider.PullRequestBranchKey},
            {"release[/-]", ConfigurationProvider.ReleaseBranchKey},
            {"hotfix[/-]", ConfigurationProvider.HotfixBranchKey },
            {"feature(s)?[/-]", ConfigurationProvider.FeatureBranchKey },
            {"feature[/-]", ConfigurationProvider.FeatureBranchKey }
        };

        public static void Notify(StringReader reader)
        {
            var deserializer = new DeserializerBuilder().WithNamingConvention(new NullNamingConvention()).IgnoreUnmatchedProperties().Build();

            var legacyConfig = deserializer.Deserialize<LegacyConfig>(reader);
            if (legacyConfig == null)
                return;

            var issues = new List<string>();

            var oldConfigs = legacyConfig.Branches.Keys.Where(k => OldConfigKnownRegexes.Keys.Contains(k) && k != OldConfigKnownRegexes[k]).ToList();
            if (oldConfigs.Any())
            {
                var max = oldConfigs.Max(c => c.Length);
                var oldBranchConfigs = oldConfigs.Select(c => string.Format("{0} -> {1}", c.PadRight(max), OldConfigKnownRegexes[c]));
                var branchErrors = string.Join("\r\n    ", oldBranchConfigs);
                issues.Add(string.Format(
@"GitVersion branch configs no longer are keyed by regexes, update:
    {0}", branchErrors));
            }

            if (legacyConfig.assemblyVersioningScheme != null)
                issues.Add("assemblyVersioningScheme has been replaced by assembly-versioning-scheme");

            if (legacyConfig.DevelopBranchTag != null)
                issues.Add("develop-branch-tag has been replaced by branch specific configuration. See http://gitversion.readthedocs.org/en/latest/configuration/#branch-configuration");

            if (legacyConfig.ReleaseBranchTag != null)
                issues.Add("release-branch-tag has been replaced by branch specific configuration. See http://gitversion.readthedocs.org/en/latest/configuration/#branch-configuration");

            if (legacyConfig.Branches != null && legacyConfig.Branches.Any(branches => branches.Value.IsDevelop != null))
                issues.Add("'is-develop' is deprecated, use 'tracks-release-branches' instead. See http://gitversion.readthedocs.org/en/latest/configuration/#branch-configuration");

            if (issues.Any())
                throw new OldConfigurationException("GitVersion configuration file contains old configuration, please fix the following errors:\r\n" + string.Join("\r\n", issues));
        }
    }
}
