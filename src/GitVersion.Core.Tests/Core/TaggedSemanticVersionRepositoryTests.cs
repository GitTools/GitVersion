using GitVersion.Configuration;
using GitVersion.Testing.Extensions;

namespace GitVersion.Tests;

[TestFixture]
public class TaggedSemanticVersionRepositoryTests : TestBase
{
    [Test]
    public void GetTaggedSemanticVersions_IgnoredAndEligibleTagsShareCommit_ReturnsOnlyEligibleTag()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.ApplyTag("1.0.0");
        fixture.ApplyTag("2.0.0");
        var sut = CreateSut(fixture);

        var actual = GetTagNames(sut, new IgnoreConfiguration { Tags = ["^2\\.0\\.0$"] });

        actual.ShouldBe(["1.0.0"]);
    }

    [Test]
    public void GetTaggedSemanticVersions_MultiplePatterns_UsesCaseInsensitiveOrSemanticsOnFriendlyName()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.ApplyTag("v1.0.0");
        fixture.ApplyTag("v2.0.0");
        fixture.ApplyTag("v3.0.0");
        var sut = CreateSut(fixture);
        var ignore = new IgnoreConfiguration { Tags = ["^v1\\.", "^V2\\.", "^refs/tags/v3"] };

        var actual = GetTagNames(sut, ignore, tagPrefix: "^v");

        actual.ShouldBe(["v3.0.0"]);
    }

    [Test]
    public void GetTaggedSemanticVersions_WhenIgnoreTagsChanges_DoesNotReuseCachedResult()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.ApplyTag("1.0.0");
        fixture.ApplyTag("2.0.0");
        var sut = CreateSut(fixture);

        var first = GetTagNames(sut, new IgnoreConfiguration { Tags = ["^2\\."] });
        var second = GetTagNames(sut, new IgnoreConfiguration { Tags = ["^1\\."] });

        first.ShouldBe(["1.0.0"]);
        second.ShouldBe(["2.0.0"]);
    }

    [TestCase(null)]
    [TestCase("^preview-")]
    public void GetTaggedSemanticVersions_EmptyOrNonMatchingTags_PreservesCandidates(string? expression)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        var sut = CreateSut(fixture);
        var ignore = new IgnoreConfiguration { Tags = expression is null ? [] : [expression] };

        var actual = GetTagNames(sut, ignore);

        actual.ShouldBe(["1.0.0"]);
    }

    private static TaggedSemanticVersionRepository CreateSut(EmptyRepositoryFixture fixture)
    {
        var repositoryStore = new RepositoryStore(
            NullLogger<RepositoryStore>.Instance,
            fixture.Repository.ToGitRepository());

        return new(NullLogger<TaggedSemanticVersionRepository>.Instance, repositoryStore);
    }

    private static string[] GetTagNames(
        TaggedSemanticVersionRepository sut,
        IIgnoreConfiguration ignore,
        string tagPrefix = "")
        => [.. sut.GetTaggedSemanticVersions(tagPrefix, SemanticVersionFormat.Strict, ignore)
            .SelectMany(group => group)
            .Select(version => version.Tag.Name.Friendly)
            .Order(StringComparer.Ordinal)];
}
