using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class MasterTests
{
    [Test]
    public void GivenARepositoryWithCommitsButNoTags_VersionShouldBe_0_1()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
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
        using (var fixture = new EmptyRepositoryFixture(new Config()))
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
        using (var fixture = new EmptyRepositoryFixture(new Config()))
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
        using (var fixture = new EmptyRepositoryFixture(new Config { NextVersion = ExpectedNextVersion }))
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.1.0+5");
        }
    }

    [Test]
    public void GivenARepositoryWithTagAndANextVersionTxtFileAndNoCommits_VersionShouldBeTag()
    {
        const string ExpectedNextVersion = "1.1.0";
        using (var fixture = new EmptyRepositoryFixture(new Config { NextVersion = ExpectedNextVersion }))
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);

            fixture.AssertFullSemver("1.0.3+0");
        }
    }

    [Test]
    public void GivenARepositoryWithTagAndNoNextVersionTxtFile_VersionShouldBeTagWithBumpedPatch()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
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
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);

            fixture.AssertFullSemver("1.0.3+0");
        }
    }

    [Test]
    public void GivenARepositoryWithTagAndOldNextVersionConfig_VersionShouldBeTagWithBumpedPatch()
    {
        const string NextVersionConfig = "1.0.0";
        using (var fixture = new EmptyRepositoryFixture(new Config { NextVersion = NextVersionConfig }))
        {
            const string TaggedVersion = "1.1.0";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.1.1+5");
        }
    }

    [Test]
    public void GivenARepositoryWithTagAndOldNextVersionConfigAndNoCommits_VersionShouldBeTag()
    {
        const string NextVersionConfig = "1.0.0";
        using (var fixture = new EmptyRepositoryFixture(new Config { NextVersion = NextVersionConfig }))
        {
            const string TaggedVersion = "1.1.0";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);

            fixture.AssertFullSemver("1.1.0+0");
        }
    }

    [Test]
    public void CanSpecifyTagPrefixes()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config { TagPrefix = "version-" }))
        {
            const string TaggedVersion = "version-1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.0.4+5");
        }
    }

    [Test]
    public void CanSpecifyTagPrefixesAsRegex()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config { TagPrefix = "[version-|v]" }))
        {
            const string TaggedVersion = "v1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.0.4+5");
        }
    }
}