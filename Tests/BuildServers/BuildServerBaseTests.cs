namespace Tests.BuildServers
{
    using System;
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
            var semanticVersion = new SemanticVersion
            {
                Major = 1,
                Minor = 2, 
                Patch = 3,
                PreReleaseTag = "beta1",
                BuildMetaData = "5"
            };

            semanticVersion.BuildMetaData.ReleaseDate = new ReleaseDate
                        {
                            OriginalCommitSha = "originalCommitSha",
                            OriginalDate = DateTimeOffset.Parse("2014-03-01 00:00:01Z"),
                            CommitSha = "commitSha",
                            Date = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
                        };

            base.WriteIntegration(semanticVersion, writes.Add);

            writes[1].ShouldBe("1.2.3-beta.1+5");
        }

        public override bool CanApplyToCurrentContext()
        {
            throw new NotImplementedException();
        }

        public override void PerformPreProcessingSteps(string gitDirectory)
        {
            throw new NotImplementedException();
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
