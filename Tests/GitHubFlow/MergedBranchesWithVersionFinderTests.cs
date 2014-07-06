using GitVersion;
using NUnit.Framework;
using Shouldly;

public class MergedBranchesWithVersionFinderTests
{
    [Test]
    public void ShouldFindMergeCommit()
    {
        var currentBranch = new MockBranch("master")
        {
            new MockCommit(),
            new MockCommit(),
            new MockMergeCommit
            {
                MessageEx = "Merge branch 'release-2.0.0'"
            }
        };
        var sut = new MergedBranchesWithVersionFinder(new GitVersionContext(null, currentBranch));

        var version = sut.GetVersion();

        version.ToString().ShouldBe("2.0.0");
    }
}