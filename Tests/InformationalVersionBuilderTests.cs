using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class InformationalVersionBuilderTests
{
    [Test]
    public void Feature()
    {
        var semanticVersion = new VersionAndBranch
                              {
                                  BranchType = BranchType.Feature,
                                  BranchName = "feature1",
                                  Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
                                  Version = new SemanticVersion
                                                       {
                                                           Major = 1,
                                                           Minor = 2,
                                                           Patch = 3,
                                                           Tag = Stability.Unstable.ToString().ToLower(),
                                                           Suffix = "a682956d",
                                                       }
                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3-unstable.feature-a682956d Branch:'feature1' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'", informationalVersion);
    }

    [Test]
    public void DevelopAlpha()
    {
        var semanticVersion = new VersionAndBranch
                              {
                                  BranchType = BranchType.Develop,
                                  BranchName = "develop",
                                  Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
                                  Version = new SemanticVersion
                                                       {
                                                           Major = 1,
                                                           Minor = 2,
                                                           Patch = 3,
                                                           Tag = "alpha645"
                                                       }
                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3-alpha645 Branch:'develop' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'", informationalVersion);
    }
    [Test]
    public void DevelopUnstable()
    {
        var semanticVersion = new VersionAndBranch
                              {
                                  BranchType = BranchType.Develop,
                                  BranchName = "develop",
                                  Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
                                  Version = new SemanticVersion
                                                       {
                                                           Major = 1,
                                                           Minor = 2,
                                                           Patch = 3,
                                                           Tag = "unstable645"
                                                       }
                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3-unstable645 Branch:'develop' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'", informationalVersion);
    }
    [Test]
    public void DevelopBeta()
    {
        var semanticVersion = new VersionAndBranch
                              {
                                  BranchType = BranchType.Develop,
                                  BranchName = "develop",
                                  Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
                                  Version = new SemanticVersion
                                                       {
                                                           Major = 1,
                                                           Minor = 2,
                                                           Patch = 3,
                                                           Tag = "beta645"
                                                       }
                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3-beta645 Branch:'develop' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'", informationalVersion);
    }

    [Test]
    public void HotfixAlpha()
    {
        var semanticVersion = new VersionAndBranch
                              {
                                  BranchType = BranchType.Hotfix,
                                  BranchName = "hotfix-foo",
                                  Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
                                  Version = new SemanticVersion
                                                       {
                                                           Major = 1,
                                                           Minor = 2,
                                                           Patch = 3,
                                                           Tag = "alpha645"
                                                       }

                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3-alpha645 Branch:'hotfix-foo' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'", informationalVersion);
    }
    [Test]
    public void HotfixBeta()
    {
        var semanticVersion = new VersionAndBranch
                              {
                                  BranchType = BranchType.Hotfix,
                                  BranchName = "hotfix-foo",
                                  Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
                                  Version = new SemanticVersion
                                                       {
                                                           Major = 1,
                                                           Minor = 2,
                                                           Patch = 3,
                                                           Tag = "beta645"
                                                       }

                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3-beta645 Branch:'hotfix-foo' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'", informationalVersion);
    }
    [Test]
    public void HotfixFinal()
    {
        var semanticVersion = new VersionAndBranch
                              {
                                  BranchType = BranchType.Hotfix,
                                  BranchName = "hotfix-foo",
                                  Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
                                  Version = new SemanticVersion
                                                       {
                                                           Major = 1,
                                                           Minor = 2,
                                                           Patch = 3,
                                                       }
                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3 Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'", informationalVersion);
    }

    [Test]
    public void Master()
    {
        var semanticVersion = new VersionAndBranch
                              {
                                  BranchType = BranchType.Master,
                                  BranchName = "master",
                                  Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
                                  Version = new SemanticVersion
                                                       {
                                                           Major = 1,
                                                           Minor = 2,
                                                           Patch = 3,
                                                       }
                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3 Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'", informationalVersion);
    }

    [Test]
    public void PullRequest()
    {
        var semanticVersion = new VersionAndBranch
                              {
                                  BranchType = BranchType.PullRequest,
                                  BranchName = "myPullRequest",
                                  Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
                                  Version = new SemanticVersion
                                                       {
                                                           Major = 1,
                                                           Minor = 2,
                                                           Patch = 3,
                                                           Tag = "unstable3"
                                                       }
                              };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.3-unstable.pull-request-3 Branch:'myPullRequest' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'", informationalVersion);
    }

    [Test]
    public void ReleaseBeta()
    {
        var semanticVersion = new VersionAndBranch
        {
            BranchType = BranchType.Release,
            BranchName = "release-1.2",
            Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
            Version = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 0,
                Tag = "beta2"
            }
        };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.0-beta2 Branch:'release-1.2' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'", informationalVersion);
    }

    [Test]
    public void ReleaseAlpha()
    {
        var semanticVersion = new VersionAndBranch
        {
            BranchType = BranchType.Release,
            BranchName = "release-1.2",
            Sha = "a682956dc1a2752aa24597a0f5cd939f93614509",
            Version = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 0,
                Tag = "alpha2",
            }
        };
        var informationalVersion = semanticVersion.ToLongString();

        Assert.AreEqual("1.2.0-alpha2 Branch:'release-1.2' Sha:'a682956dc1a2752aa24597a0f5cd939f93614509'", informationalVersion);
    }
}