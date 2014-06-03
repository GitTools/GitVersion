using GitVersion;
using NUnit.Framework;

[TestFixture]
public class InformationalVersionBuilderTests
{
    [TestCase(BranchType.Feature, "feature1", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable", 1, "1.2.3-unstable+1.Branch.feature1.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase(BranchType.Develop, "develop", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "alpha645", null, "1.2.3-alpha.645+Branch.develop.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase(BranchType.Develop, "develop", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable645", null, "1.2.3-unstable.645+Branch.develop.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase(BranchType.Develop, "develop", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "beta645", null, "1.2.3-beta.645+Branch.develop.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase(BranchType.Hotfix, "hotfix-foo", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "alpha645", null, "1.2.3-alpha.645+Branch.hotfix-foo.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase(BranchType.Hotfix, "hotfix-foo", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "beta645", null, "1.2.3-beta.645+Branch.hotfix-foo.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase(BranchType.Hotfix, "hotfix-foo", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, null, null, "1.2.3+Branch.hotfix-foo.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase(BranchType.Master, "master", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, null, null, "1.2.3+Branch.master.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase(BranchType.PullRequest, "myPullRequest", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable3", null, "1.2.3-unstable.3+Branch.myPullRequest.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase(BranchType.Release, "release-1.2", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 0, "beta2", null, "1.2.0-beta.2+Branch.release-1.2.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase(BranchType.Release, "release-1.2", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 0, "alpha2", null, "1.2.0-alpha.2+Branch.release-1.2.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    public void ValidateInformationalVersionBuilder(BranchType branchType, string branchName, string sha, int major, int minor, int patch,
        string tag, int? suffix, string versionString)
    {
        var semanticVersion = new SemanticVersion
        {
            Major = major,
            Minor = minor,
            Patch = patch,
            PreReleaseTag = tag,
            BuildMetaData = new SemanticVersionBuildMetaData(suffix, branchName, new ReleaseDate{ CommitSha = sha }),
        };
        var informationalVersion = semanticVersion.ToString("i");

        Assert.AreEqual(versionString, informationalVersion);
    }

    [TestCase("feature1", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable", 1, "1.2.3-feature1")]
    [TestCase("feature-TKT-99999", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable", 1, "1.2.3-TKT-99999")]
    [TestCase("feature-GiantRefatorWithLongName", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable", 1, "1.2.3-GiantRefatorWithLong")]
    [TestCase("feature-add_tests/nunit", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable", 1, "1.2.3-add_tests-nunit")]
    [TestCase("develop", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "alpha645", null, "1.2.3-alpha0645")]
    [TestCase("develop", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable645", null, "1.2.3-unstable0645")]
    [TestCase("develop", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "beta645", null, "1.2.3-beta0645")]
    [TestCase("hotfix-foo", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "alpha645", null, "1.2.3-alpha0645+0")]
    [TestCase("hotfix-foo", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "beta645", null, "1.2.3-beta0645+0")]
    [TestCase("hotfix-foo", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, null, null, "1.2.3+0")]
    [TestCase("master", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, null, null, "1.2.3")]
    [TestCase("myPullRequest", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable3", null, "1.2.3-myPullRequest")]
    [TestCase("myPullRequestWithLongName", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable3", null, "1.2.3-myPullRequestWithLon")]
    [TestCase("release-1.2", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 0, "beta2", null, "1.2.0-beta0002+0")]
    [TestCase("release-1.2", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 0, "alpha2", null, "1.2.0-alpha0002+0")]
    [TestCase("release-1.2", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 0, null, null, "1.2.0+0")]
    public void ValidateNugetVersionBuilder(string branchName, string sha, int major, int minor, int patch,
        string tag, int? suffix, string versionString)
    {
        var semanticVersion = new SemanticVersion
        {
            Major = major,
            Minor = minor,
            Patch = patch,
            PreReleaseTag = tag,
            BuildMetaData = new SemanticVersionBuildMetaData(suffix, branchName, new ReleaseDate { CommitSha = sha }),
        };
        var nugetVersion = semanticVersion.ToString("n");

        Assert.AreEqual(versionString, nugetVersion);
    }
}