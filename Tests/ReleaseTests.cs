using System;
using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class ReleaseTests
{
    [Test]
    public void No_commits()
    {
        var version = FinderWrapper.FindVersionForCommit("c50179a2c77843245ace262b51b08af7b3b7f8fe", "release-0.3.0");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(3, version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stage.Beta, version.Stage);
        Assert.AreEqual(0, version.PreRelease, "Prerelease should be set to 0 since there is no commits");
    }

    [Test]
    public void First_commit()
    {
        var version = FinderWrapper.FindVersionForCommit("d2f82c724005bc7c5c50106f0577606ea4f89e80", "release-0.4.0");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(4, version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stage.Beta, version.Stage);
        Assert.AreEqual(1, version.PreRelease, "Prerelease should be set to 1 since there is a commit on the branch");
    }

    [Test]
    public void Second_commit()
    {
        var version = FinderWrapper.FindVersionForCommit("d27a07a5522980d53b08ae0141b281ab31570533", "release-0.4.0");
        Assert.AreEqual(0, version.Major);
        Assert.AreEqual(4, version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(Stage.Beta, version.Stage);
        Assert.AreEqual(2, version.PreRelease, "Prerelease should be set to 2 since there is 2 commits on the branch");
    }

    [Test, Ignore("Not really going to happen in real life se we skip this for now")]
    public void After_merge_to_master()
    {
        Assert.Throws<Exception>(() => FinderWrapper.FindVersionForCommit("TODO", "release-0.5.0"));
    }

}