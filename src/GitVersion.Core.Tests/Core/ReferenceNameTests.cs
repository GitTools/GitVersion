using GitVersion.Git;

namespace GitVersion.Tests;

[TestFixture]
public class ReferenceNameTests
{
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
