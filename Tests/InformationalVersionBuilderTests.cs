using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class InformationalVersionBuilderTests
{
    [TestCase(BranchType.Feature, "feature1", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable", "a682956d",
              "1.2.3-unstable.feature-a682956d Branch:'feature1' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'")]
    [TestCase(BranchType.Develop, "develop", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "alpha645", null,
              "1.2.3-alpha645 Branch:'develop' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'")]
    [TestCase(BranchType.Develop, "develop", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable645", null,
              "1.2.3-unstable645 Branch:'develop' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'")]
    [TestCase(BranchType.Develop, "develop", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "beta645", null,
              "1.2.3-beta645 Branch:'develop' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'")]
    [TestCase(BranchType.Hotfix, "hotfix-foo", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "alpha645", null,
              "1.2.3-alpha645 Branch:'hotfix-foo' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'")]
    [TestCase(BranchType.Hotfix, "hotfix-foo", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "beta645", null,
              "1.2.3-beta645 Branch:'hotfix-foo' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'")]
    [TestCase(BranchType.Hotfix, "hotfix-foo", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, null, null,
              "1.2.3 Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'")]
    [TestCase(BranchType.Master, "master", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, null, null,
              "1.2.3 Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'")]
    [TestCase(BranchType.PullRequest, "myPullRequest", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable3", null,
              "1.2.3-unstable.pull-request-3 Branch:'myPullRequest' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'")]
    [TestCase(BranchType.Release, "release-1.2", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 0, "beta2", null,
              "1.2.0-beta2 Branch:'release-1.2' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'")]
    [TestCase(BranchType.Release, "release-1.2", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 0, "alpha2", null,
              "1.2.0-alpha2 Branch:'release-1.2' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'")]
    public void ValidateInformationalVersionBuilder(BranchType branchType, string branchName, string sha, int major, int minor, int patch,
        string tag, string suffix, string versionString)
    {
        var semanticVersion = new VersionAndBranch
        {
            BranchType = branchType,
            BranchName = branchName,
            Sha = sha,
            Version = new SemanticVersion
            {
                Major = major,
                Minor = minor,
                Patch = patch,
                Tag = tag,
                Suffix = suffix,
            }
        };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual(versionString, informationalVersion);
    }

}