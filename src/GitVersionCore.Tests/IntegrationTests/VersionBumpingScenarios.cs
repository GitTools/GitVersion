using GitTools.Testing;
using GitVersion;
using GitVersionCore.Tests;
using NUnit.Framework;

[TestFixture]
public class VersionBumpingScenarios
{
    [Test]
    public void AppliedPrereleaseTagCausesBump()
    {
        var configuration = new Config
        {
            Branches =
            {
                { "master", new BranchConfig { Tag = "pre" } }
            }
        };
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeATaggedCommit("1.0.0-pre.1");
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver(configuration, "1.0.0-pre.2+1");
        }
    }
}
