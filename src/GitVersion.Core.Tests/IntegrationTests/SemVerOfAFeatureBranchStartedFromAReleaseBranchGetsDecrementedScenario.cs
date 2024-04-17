namespace GitVersion.Core.Tests.IntegrationTests;

/// <summary>
/// [Bug] SemVer of a feature branch started from a release branch gets decremented #3151
/// </summary>
[TestFixture]
public class SemVerOfAFeatureBranchStartedFromAReleaseBranchGetsDecrementedScenario
{
    [Test]
    public void ShouldPickUpReleaseVersionAfterCreatedFromRelease()
    {
        using var fixture = new EmptyRepositoryFixture();

        // Create develop and a release branch
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.BranchTo("release/1.1.0");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-beta.1+2");

        // Create a feature branch from the release/1.1.0 branch
        fixture.BranchTo("feature/test");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-test.1+3");
    }
}
