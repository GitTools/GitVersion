using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class InformationalVersionBuilderTests
{
    [Test]
    public void Feature()
    {
        var semanticVersion = new VersionInformation
                              {
                                  BranchType = BranchType.Feature,
                                  Major = 1,
                                  Minor = 2,
                                  Patch = 3,
                                  //PreRelease = 645,
                                  Stability = Stability.Unstable,
                                  Suffix = "a682956d",
                                  BranchName = "feature1",
                                  Sha = "a682956dccae752aa24597a0f5cd939f93614509"
                                  
                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3-unstable.feature-a682956d Branch:'feature1' Sha:'a682956dccae752aa24597a0f5cd939f93614509'", informationalVersion);
    }
    [Test]
    public void Develop()
    {
        var semanticVersion = new VersionInformation
                              {
                                  BranchType = BranchType.Develop,
                                  Major = 1,
                                  Minor = 2,
                                  Patch = 3,
                                  PreReleaseNumber = 645,
                                  Stability = Stability.Unstable,
                                  BranchName = "develop",
                                  Sha = "a682956dccae752aa24597a0f5cd939f93614509"
                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3-unstable645 Branch:'develop' Sha:'a682956dccae752aa24597a0f5cd939f93614509'", informationalVersion);
    }

    [Test]
    public void Hotfix()
    {
        var semanticVersion = new VersionInformation
                              {
                                  BranchType = BranchType.Hotfix,
                                  Major = 1,
                                  Minor = 2,
                                  Patch = 3,
                                  PreReleaseNumber = 645,
                                  Stability = Stability.Beta,
                                  BranchName = "hotfix-foo",
                                  Sha = "a682956dccae752aa24597a0f5cd939f93614509"

                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3-beta645 Branch:'hotfix-foo' Sha:'a682956dccae752aa24597a0f5cd939f93614509'", informationalVersion);
    }

    [Test]
    public void Master()
    {
        var semanticVersion = new VersionInformation
                              {
                                  BranchType = BranchType.Master,
                                  Major = 1,
                                  Minor = 2,
                                  Patch = 3,
                                  Stability = Stability.Final,
                                  BranchName = "master",
                                  Sha = "a682956dccae752aa24597a0f5cd939f93614509"
                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3 Sha:'a682956dccae752aa24597a0f5cd939f93614509'", informationalVersion);
    }
    [Test]
    public void PullRequest()
    {
        var semanticVersion = new VersionInformation
                              {
                                  BranchType = BranchType.PullRequest,
                                  Major = 1,
                                  Minor = 2,
                                  Patch = 3,
                                  PreReleaseNumber = 3,
                                  Stability = Stability.Unstable,
                                  BranchName = "myPullRequest",
                                  Sha = "a682956dccae752aa24597a0f5cd939f93614509"

                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3-unstable.pull-request-3 Branch:'myPullRequest' Sha:'a682956dccae752aa24597a0f5cd939f93614509'", informationalVersion);
    }
    [Test]
    public void Release()
    {
        var semanticVersion = new VersionInformation
                              {
                                  BranchType = BranchType.Release,
                                  Major = 1,
                                  Minor = 2,
                                  Patch = 0,
                                  PreReleaseNumber = 2,
                                  Stability = Stability.Beta,
                                  BranchName = "release-1.2",
                                  Sha = "a682956dccae752aa24597a0f5cd939f93614509"

                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.0-beta2 Branch:'release-1.2' Sha:'a682956dccae752aa24597a0f5cd939f93614509'", informationalVersion);
    }
}