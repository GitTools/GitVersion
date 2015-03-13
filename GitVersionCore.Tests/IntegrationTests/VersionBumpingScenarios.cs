using GitVersion;
using NUnit.Framework;

[TestFixture]
public class VersionBumpingScenarios
{
    [Test]
    public void AppliedPrereleaseTagCausesBump()
    {
        var configuration = new Config();
        configuration.Branches["master"].Tag = "pre";
        using (var fixture = new EmptyRepositoryFixture(configuration))
        {
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeATaggedCommit("1.0.0-pre.1");
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.0.0-pre.2+1");
        }
    }
}
