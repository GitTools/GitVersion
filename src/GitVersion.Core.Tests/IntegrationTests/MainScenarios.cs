using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class MainScenarios : TestBase
{
    [Test]
    public void CanHandleContinuousDelivery()
    {
        var config = new Config
        {
            Branches =
            {
                {
                    MainBranch, new BranchConfig
                    {
                        VersioningMode = VersioningMode.ContinuousDelivery
                    }
                }
            }
        };
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("1.0.1+2", config);
    }

    [Test]
    public void CanHandleContinuousDeployment()
    {
        var config = new Config
        {
            Branches =
            {
                {
                    MainBranch, new BranchConfig
                    {
                        VersioningMode = VersioningMode.ContinuousDeployment
                    }
                }
            }
        };
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("1.0.1-ci.2", config);
    }

    [Test]
    public void GivenARepositoryWithCommitsButNoTagsVersionShouldBe01()
    {
        using var fixture = new EmptyRepositoryFixture();
        // Given
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();

        // When
        fixture.AssertFullSemver("0.0.1+3");
    }

    [Test]
    public void GivenARepositoryWithCommitsButBadTagsVersionShouldBe01()
    {
        using var fixture = new EmptyRepositoryFixture();
        // Given
        fixture.Repository.MakeACommit();
        fixture.Repository.ApplyTag("BadTag");
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();

        // When
        fixture.AssertFullSemver("0.0.1+3");
    }

    [Test]
    public void GivenARepositoryWithCommitsButNoTagsWithDetachedHeadVersionShouldBe01()
    {
        using var fixture = new EmptyRepositoryFixture();
        // Given
        fixture.Repository.MakeACommit(); // one
        fixture.Repository.MakeACommit(); // two
        fixture.Repository.MakeACommit(); // three

        var commit = fixture.Repository.Head.Tip;
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, commit);

        // When
        fixture.AssertFullSemver("0.0.1+3", onlyTrackedBranches: false);
    }

    [Test]
    public void GivenARepositoryWithTagAndNextVersionInConfigVersionShouldMatchVersionTxtFile()
    {
        const string expectedNextVersion = "1.1.0";
        var config = new Config { NextVersion = expectedNextVersion };
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.1.0+5", config);
    }

    [Test]
    public void GivenARepositoryWithTagAndANextVersionTxtFileAndNoCommitsVersionShouldBeTag()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.AssertFullSemver("1.0.3");
        // I'm not sure if the postfix +0 is correct here... Maybe it should always disappear when it is zero?
        // but the next version configuration property is something for the user to manipulate the resulting version.
        fixture.AssertFullSemver("1.1.0+0", new Config { NextVersion = "1.1.0" });
    }

    [Test]
    public void GivenARepositoryWithTagAndANextVersionTxtFileAndNoCommitsVersionShouldBeTag2()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.0.4+1");

        // I'm not sure if the postfix +1 is correct here...
        // but the next version configuration property is something for the user to manipulate the resulting version.
        fixture.AssertFullSemver("1.1.0+1", new Config { NextVersion = "1.1.0" });
    }

    [Test]
    public void GivenARepositoryWithTagAndANextVersionTxtFileAndNoCommitsVersionShouldBeTag3()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.AssertFullSemver("1.0.3");
        fixture.AssertFullSemver("1.0.3", new Config { NextVersion = "1.0.2" });
    }

    [Test]
    public void GivenARepositoryWithTagAndANextVersionTxtFileAndNoCommitsVersionShouldBeTag4()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.0.4+1");
        fixture.AssertFullSemver("1.0.4+1", new Config { NextVersion = "1.0.4" });
    }

    [Test]
    public void GivenARepositoryWithTagAndNoNextVersionTxtFileVersionShouldBeTagWithBumpedPatch()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.4+5");
    }

    [Test]
    public void GivenARepositoryWithTagAndNoNextVersionTxtFileAndNoCommitsVersionShouldBeTag()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);

        fixture.AssertFullSemver("1.0.3");
    }

    [Test]
    public void GivenARepositoryWithTagAndOldNextVersionConfigVersionShouldBeTagWithBumpedPatch()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.1.0";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.1.1+5", new Config { NextVersion = "1.0.0" });
    }

    [Test]
    public void GivenARepositoryWithTagAndOldNextVersionConfigAndNoCommitsVersionShouldBeTag()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.1.0";
        fixture.Repository.MakeATaggedCommit(taggedVersion);

        fixture.AssertFullSemver("1.1.0", new Config { NextVersion = "1.0.0" });
    }

    [Test]
    public void CanSpecifyTagPrefixes()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "version-1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.4+5", new Config { TagPrefix = "version-" });
    }

    [Test]
    public void CanSpecifyTagPrefixesAsRegex()
    {
        var config = new Config { TagPrefix = "version-|[vV]" };
        using var fixture = new EmptyRepositoryFixture();
        var taggedVersion = "v1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.4+5", config);

        taggedVersion = "version-1.0.5";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.6+5", config);
    }

    [Test]
    public void AreTagsNotAdheringToTagPrefixIgnored()
    {
        var config = new Config { TagPrefix = "" };
        using var fixture = new EmptyRepositoryFixture();
        var taggedVersion = "version-1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion); // one
        fixture.Repository.MakeCommits(5); // two, thre, four, five, six right?

        fixture.AssertFullSemver("0.0.1+6", config);    // 6 commits

        taggedVersion = "bad/1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion); // seven

        fixture.AssertFullSemver("0.0.1+7", config);   // 7 commits
    }
}
