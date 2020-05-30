using System;
using GitVersion.Model.Configuration;

namespace GitVersionCore.Tests.Helpers
{
    [Obsolete("Do not use that config because it implicitly overwrites some settings (even default values). Use Config and override required settings explicitly instead.")]
    public class TestableConfig : Config
    {
        public override void MergeTo(Config targetConfig)
        {
            targetConfig.Ignore = this.Ignore;

            targetConfig.Branches.Clear();
            targetConfig.Branches = this.Branches;

            targetConfig.Increment = this.Increment;
            targetConfig.NextVersion = this.NextVersion;
            targetConfig.VersioningMode = this.VersioningMode;
            targetConfig.AssemblyFileVersioningFormat = this.AssemblyFileVersioningFormat;
            targetConfig.TagPrefix = this.TagPrefix;
            targetConfig.TagPreReleaseWeight = this.TagPreReleaseWeight;
        }
    }
}
