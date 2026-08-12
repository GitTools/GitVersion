namespace GitVersion.Tests;

[TestFixture]
public class RepositoryFixtureBaseTests
{
    [Test]
    public void DoesNotRecordDeletionWhenBranchRemovalFails()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        var currentBranch = fixture.Repository.Head.FriendlyName;
        var diagram = fixture.SequenceDiagram.GetDiagram();

        Should.Throw<Exception>(() => fixture.Remove(currentBranch));

        fixture.SequenceDiagram.GetDiagram().ShouldBe(diagram);
    }
}
