using GitTools.Testing;
using GitVersion;
using GitVersionCore.Tests;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class MasterScenarios
{
    [Test]
    public void CanHandleContinuousDelivery()
    {
        var config = new Config
        {
            Branches =
            {
                {
                    "master", new BranchConfig
                    {
                        VersioningMode = VersioningMode.ContinuousDelivery
                    }
                }
            }
        };
        using(var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver(config, "1.0.1+2");
        }
    }

    [Test]
    public void CanHandleContinuousDeployment()
    {
        var config = new Config
        {
            Branches =
            {
                {
                    "master", new BranchConfig
                    {
                        VersioningMode = VersioningMode.ContinuousDeployment
                    }
                }
            }
        };
        using(var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeCommits(2);
            fixture.AssertFullSemver(config, "1.0.1-ci.2");
        }
    }

    [Test]
    public void GivenARepositoryWithCommitsButNoTags_VersionShouldBe_0_1()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            // Given
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            // When
            fixture.AssertFullSemver("0.1.0+2");
        }
    }

    [Test]
    public void GivenARepositoryWithCommitsButBadTags_VersionShouldBe_0_1()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            // Given
            fixture.Repository.MakeACommit();
            fixture.Repository.ApplyTag("BadTag");
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            // When
            fixture.AssertFullSemver("0.1.0+2");
        }
    }

    [Test]
    public void GivenARepositoryWithCommitsButNoTagsWithDetachedHead_VersionShouldBe_0_1()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            // Given
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeACommit();

            var commit = fixture.Repository.Head.Tip;
            fixture.Repository.MakeACommit();
            fixture.Repository.Checkout(commit);

            // When
            fixture.AssertFullSemver("0.1.0+2");
        }
    }

    [Test]
    public void GivenARepositoryWithTagAndNextVersionInConfig_VersionShouldMatchVersionTxtFile()
    {
        const string ExpectedNextVersion = "1.1.0";
        var config = new Config { NextVersion = ExpectedNextVersion };
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver(config, "1.1.0+5");
        }
    }

    [Test]
    public void GivenARepositoryWithTagAndANextVersionTxtFileAndNoCommits_VersionShouldBeTag()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);

            fixture.AssertFullSemver(new Config { NextVersion = "1.1.0" }, "1.0.3");
        }
    }

    [Test]
    public void GivenARepositoryWithTagAndNoNextVersionTxtFile_VersionShouldBeTagWithBumpedPatch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.0.4+5");
        }
    }

    [Test]
    public void GivenARepositoryWithTagAndNoNextVersionTxtFileAndNoCommits_VersionShouldBeTag()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);

            fixture.AssertFullSemver("1.0.3");
        }
    }

    [Test]
    public void GivenARepositoryWithTagAndOldNextVersionConfig_VersionShouldBeTagWithBumpedPatch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "1.1.0";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver(new Config { NextVersion = "1.0.0" }, "1.1.1+5");
        }
    }

    [Test]
    public void GivenARepositoryWithTagAndOldNextVersionConfigAndNoCommits_VersionShouldBeTag()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "1.1.0";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);

            fixture.AssertFullSemver(new Config { NextVersion = "1.0.0" }, "1.1.0");
        }
    }

    [Test]
    public void CanSpecifyTagPrefixes()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "version-1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver(new Config { TagPrefix = "version-" }, "1.0.4+5");
        }
    }    

    [Test]
    public void CanSpecifyTagPrefixesAsRegex()
    {
        var config = new Config { TagPrefix = "version-|[vV]" };
        using (var fixture = new EmptyRepositoryFixture())
        {
            var TaggedVersion = "v1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver(config, "1.0.4+5");

            TaggedVersion = "version-1.0.5";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver(config, "1.0.6+5");
        }
    }

    [Test]
    public void AreTagsNotAdheringToTagPrefixIgnored()
    {
        var config = new Config { TagPrefix = "" };
        using (var fixture = new EmptyRepositoryFixture())
        {
            var TaggedVersion = "version-1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver(config, "0.1.0+5");    //Fallback version + 5 commits since tag

            TaggedVersion = "bad/1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);

            fixture.AssertFullSemver(config, "0.1.0+6");   //Fallback version + 6 commits since tag
        }
    }
}