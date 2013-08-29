using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class MasterTests
{

    [Test]
    public void Commit_on_tag_should_return_tag_as_version()
    {
        var version = FinderWrapper.FindVersionForCommit("a682956dccae752aa24597a0f5cd939f93614509", "master");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stage.Final, version.Stage);
        Assert.AreEqual(0, version.PreRelease);
    }

    [Test]
    public void Commit_in_front_of_tag_should_return_tag_as_version()
    {
        var version = FinderWrapper.FindVersionForCommit("6b503e747408bbbcac7ec20a6c81cf10e53b6dcd", "master");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stage.Final, version.Stage);
        Assert.AreEqual(0, version.PreRelease);
    }
}