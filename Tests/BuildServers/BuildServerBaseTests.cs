namespace Tests.BuildServers
{
    using System.Collections.Generic;
    using GitVersion;
    using NUnit.Framework;
    using Shouldly;

    public class BuildServerBaseTests : BuildServerBase
    {
        [Test]
        public void BuildNumberIsFullSemVer()
        {
            var writes = new List<string>();
            base.WriteIntegration(new SemanticVersion
            {
                Major = 1,
                Minor = 2, 
                Patch = 3,
                PreReleaseTag = "beta1",
                BuildMetaData = "5"
            }, writes.Add);

            writes[1].ShouldBe("1.2.3-beta.1+5");
        }

        public override bool CanApplyToCurrentContext()
        {
            throw new System.NotImplementedException();
        }

        public override void PerformPreProcessingSteps(string gitDirectory)
        {
            throw new System.NotImplementedException();
        }

        public override string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            return versionToUseForBuildNumber;
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new string[0];
        }
    }
}