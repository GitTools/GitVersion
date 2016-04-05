namespace GitVersion
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    public class LegacyConfigNotifier
    {
        public static void Notify(StringReader reader)
        {
            var deserializer = new Deserializer(null, new NullNamingConvention(), ignoreUnmatched: true);
            var legacyConfig = deserializer.Deserialize<LegacyConfig>(reader);
            if (legacyConfig == null)
                return;

            var issues = new List<string>();

            if (legacyConfig.assemblyVersioningScheme != null)
                issues.Add("assemblyVersioningScheme has been replaced by assembly-versioning-scheme");

            if (legacyConfig.DevelopBranchTag != null)
                issues.Add("develop-branch-tag has been replaced by branch specific configuration. See http://gitversion.readthedocs.org/en/latest/configuration/#branch-configuration");

            if (legacyConfig.ReleaseBranchTag != null)
                issues.Add("release-branch-tag has been replaced by branch specific configuration. See http://gitversion.readthedocs.org/en/latest/configuration/#branch-configuration");

            if (issues.Any())
                throw new OldConfigurationException("GitVersion configuration file contains old configuration, please fix the following errors:\r\n" + string.Join("\r\n", issues));
        }
    }
}