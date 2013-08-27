using NUnit.Framework;

[TestFixture]
public class MasterTests
{

    [Test]
    public void Commit_on_tag_should_return_tag_as_version()
    {
        var version = FinderWrapper.FindVersionForCommit("2b69755877b8d730bc49d8f7d30598f8faed9cf4", "master");
        Assert.AreEqual("0.1.0".ToVersion(), version);
    }
    [Test]
    public void Commit_in_front_of_tag_should_return_tag_as_version()
    {
        var version = FinderWrapper.FindVersionForCommit("df4c5761bac2c283c8369cfacc398f1c742a8e87", "master");
        Assert.AreEqual("0.1.0".ToVersion(), version);
    }
}