using GitVersion.Git;

namespace GitVersion.Tests;

[TestFixture]
public class ReferenceNameTests
{
    [TestCase("1.2.3", "refs/tags/1.2.3")]
    [TestCase("release/1.2.3", "refs/tags/release/1.2.3")]
    [TestCase("refs/tags/1.2.3", "refs/tags/refs/tags/1.2.3")]
    [TestCase("refs/heads/main", "refs/tags/refs/heads/main")]
    public void FromTagName_PreservesLiteralName(string tagName, string canonicalName)
    {
        var referenceName = ReferenceName.FromTagName(tagName);

        referenceName.Canonical.ShouldBe(canonicalName);
        referenceName.Friendly.ShouldBe(tagName);
        referenceName.IsTag.ShouldBeTrue();
        referenceName.IsLocalBranch.ShouldBeFalse();
    }

    [Test]
    public void FromTagName_RejectsNull() =>
        Should.Throw<ArgumentNullException>(() => ReferenceName.FromTagName(null!)).ParamName.ShouldBe("tagName");

    [Test]
    public void FromTagName_RejectsEmpty() =>
        Should.Throw<ArgumentException>(() => ReferenceName.FromTagName("")).ParamName.ShouldBe("tagName");

    [TestCase("refs/remotes/origin/release/1.0.0", "release/1.0.0", true)]
    [TestCase("refs/remotes/upstream/release/1.0.0", "release/1.0.0", false)]
    public void EquivalentTo_UsesOriginStrippedName(string canonicalName, string name, bool expected)
    {
        var referenceName = new ReferenceName(canonicalName);

        referenceName.EquivalentTo(name).ShouldBe(expected);
    }

    [TestCase("refs/heads/release/1.0.0", "release/1.0.0")]
    [TestCase("refs/remotes/origin/release/1.0.0", "release/1.0.0")]
    [TestCase("refs/remotes/upstream/release/1.0.0", "upstream/release/1.0.0")]
    [TestCase("refs/remotes/pull/123/merge", "pull/123/merge")]
    [TestCase("refs/pull/123/merge", "refs/pull/123/merge")]
    public void WithoutOrigin_ReturnsExpectedName(string canonicalName, string expected)
    {
        var referenceName = new ReferenceName(canonicalName);

        referenceName.WithoutOrigin.ShouldBe(expected);
    }

    [TestCase("refs/heads/release/1.0.0", "release/1.0.0")]
    [TestCase("refs/remotes/origin/release/1.0.0", "release/1.0.0")]
    [TestCase("refs/remotes/upstream/release/1.0.0", "release/1.0.0")]
    [TestCase("refs/remotes/pull/123/merge", "pull/123/merge")]
    [TestCase("refs/pull/123/merge", "refs/pull/123/merge")]
    public void WithoutRemote_ReturnsExpectedName(string canonicalName, string expected)
    {
        var referenceName = new ReferenceName(canonicalName);

        referenceName.WithoutRemote.ShouldBe(expected);
    }
}
