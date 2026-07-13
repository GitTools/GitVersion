using GitVersion.Helpers;

namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class GitConfigurationFileTests
{
    private static GitConfigurationFile Load(string content)
    {
        var directory = FileSystemHelper.Path.GetRepositoryTempPath();
        System.IO.Directory.CreateDirectory(directory);
        var path = FileSystemHelper.Path.Combine(directory, "config");
        File.WriteAllText(path, content);
        try
        {
            return GitConfigurationFile.Load(path);
        }
        finally
        {
            FileSystemHelper.Directory.DeleteDirectory(directory);
        }
    }

    [Test]
    public void UnescapesBackslashesInUnquotedValue()
    {
        // git stores a Windows remote path with escaped backslashes and no surrounding quotes.
        var config = Load("[remote \"origin\"]\n\turl = D:\\\\a\\\\_temp\\\\repo\n");

        config.GetString("remote", "origin", "url").ShouldBe(@"D:\a\_temp\repo");
    }

    [Test]
    public void UnescapesBackslashesInQuotedValue()
    {
        var config = Load("[remote \"origin\"]\n\turl = \"D:\\\\a\\\\repo\"\n");

        config.GetString("remote", "origin", "url").ShouldBe(@"D:\a\repo");
    }

    [Test]
    public void KeepsForwardSlashPathUnchanged()
    {
        var config = Load("[remote \"origin\"]\n\turl = /tmp/repositories/repo\n");

        config.GetString("remote", "origin", "url").ShouldBe("/tmp/repositories/repo");
    }

    [Test]
    public void StripsInlineCommentOutsideQuotesButNotInside()
    {
        Load("[core]\n\tx = value ; trailing\n").GetString("core", null, "x").ShouldBe("value");
        Load("[core]\n\tx = \"a ; b\"\n").GetString("core", null, "x").ShouldBe("a ; b");
    }

    [Test]
    public void DecodesEscapeSequences()
    {
        var config = Load("[core]\n\tx = a\\tb\\nc\n");

        config.GetString("core", null, "x").ShouldBe("a\tb\nc");
    }

    [Test]
    public void TrimsUnquotedTrailingWhitespaceButKeepsQuotedWhitespace()
    {
        Load("[core]\n\tx = value   \n").GetString("core", null, "x").ShouldBe("value");
        Load("[core]\n\tx = \"value   \"\n").GetString("core", null, "x").ShouldBe("value   ");
    }
}
