using System;
using GitVersion;
using GitVersionCore.Tests;
using NUnit.Framework;

[TestFixture]
public class InformationalVersionBuilderTests : TestBase
{
    [TestCase("feature1", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable", 1, "1.2.3-unstable+1.Branch.feature1.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase("develop", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "alpha645", null, "1.2.3-alpha.645+Branch.develop.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase("develop", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable645", null, "1.2.3-unstable.645+Branch.develop.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase("develop", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "beta645", null, "1.2.3-beta.645+Branch.develop.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase("hotfix-foo", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "alpha645", null, "1.2.3-alpha.645+Branch.hotfix-foo.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase("hotfix-foo", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "beta645", null, "1.2.3-beta.645+Branch.hotfix-foo.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase("hotfix-foo", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, null, null, "1.2.3+Branch.hotfix-foo.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase("master", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, null, null, "1.2.3+Branch.master.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase("myPullRequest", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 3, "unstable3", null, "1.2.3-unstable.3+Branch.myPullRequest.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase("release-1.2", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 0, "beta2", null, "1.2.0-beta.2+Branch.release-1.2.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    [TestCase("release-1.2", "a682956dc1a2752aa24597a0f5cd939f93614509", 1, 2, 0, "alpha2", null, "1.2.0-alpha.2+Branch.release-1.2.Sha.a682956dc1a2752aa24597a0f5cd939f93614509")]
    public void ValidateInformationalVersionBuilder(string branchName, string sha, int major, int minor, int patch,
        string tag, int? suffix, string versionString)
    {
        var semanticVersion = new SemanticVersion
        {
            Major = major,
            Minor = minor,
            Patch = patch,
            PreReleaseTag = tag,
            BuildMetaData = new SemanticVersionBuildMetaData(suffix, branchName,sha,DateTimeOffset.MinValue),
        };
        var informationalVersion = semanticVersion.ToString("i");

        Assert.AreEqual(versionString, informationalVersion);
    }

}