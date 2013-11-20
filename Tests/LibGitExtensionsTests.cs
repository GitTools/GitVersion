using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class LibGitExtensionsTests
{
    [Test]
    public void NewestSemVerTag_RetrieveTheHighestSemanticVersionPointingAtTheSpecifiedCommit()
    {
        var tagNames = new[]{"a", "9.0.0", "z", "0.1.0", "11.1.0", "0.2.0"};
        var mockCommit = new MockCommit();

        var col = new MockTagCollection();
        foreach (var tagName in tagNames)
        {
            col.Add(new MockTag
                    {
                        NameEx = tagName,
                        TargetEx = mockCommit
                    });
        }

        IRepository repo = new MockRepository { Tags = col };

        var sv = repo.NewestSemVerTag(mockCommit);

        Assert.AreEqual(11, sv.Major);
        Assert.AreEqual(1, sv.Minor);
        Assert.AreEqual(0, sv.Patch);
    }

    [Test]
    public void NewestSemVerTag_ReturnNullWhenNoTagPointingAtTheSpecifiedCommitHasBeenFound()
    {
        var tagNames = new[] { "a", "9.0.0", "z", "0.1.0", "11.1.0", "0.2.0" };

        var col = new MockTagCollection();
        foreach (var tagName in tagNames)
        {
            col.Add(new MockTag
            {
                NameEx = tagName,
                TargetEx = null
            });
        }

        IRepository repo = new MockRepository { Tags = col };

        var sv = repo.NewestSemVerTag(new MockCommit());

        Assert.IsNull(sv);
    }
}
