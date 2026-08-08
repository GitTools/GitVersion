namespace GitVersion.Tests;

[TestFixture]
public class SequenceDiagramTests
{
    [Test]
    public void CreatesMermaidSequenceDiagram()
    {
        var diagram = new SequenceDiagram();

        diagram.GetDiagram().ShouldBe("sequenceDiagram\n");
    }

    [Test]
    public void CreatesParticipantsAndRepositoryEvents()
    {
        var diagram = new SequenceDiagram();

        diagram.Participant("main");
        diagram.BranchTo("feature/foo", "main", "feature");
        diagram.Activate("feature/foo");
        diagram.MakeACommit("feature/foo");
        diagram.ApplyTag("1.2.3", "feature/foo");
        diagram.Merge("feature/foo", "main");
        diagram.Deactivate("feature/foo");
        diagram.Destroy("feature/foo", "main");

        diagram.GetDiagram().ShouldBe(
            """
            sequenceDiagram
                participant main
                create participant feature as feature/foo
                main->>feature: branch from main
                activate feature
                feature->>feature: commit
                feature->>feature: tag 1.2.3
                feature->>main: merge
                deactivate feature
                destroy feature
                main--xfeature: delete branch

            """);
    }

    [Test]
    public void CreatesSpanningMultilineNoteWithBackgroundColor()
    {
        var diagram = new SequenceDiagram();

        diagram.Participant("main");
        diagram.Participant("support/1.x", "support");
        diagram.NoteOver("Version 1.2.3; stable\r\nReady", "main", "support/1.x", "#D3D3D3");

        diagram.GetDiagram().ShouldBe(
            """
            sequenceDiagram
                participant main
                participant support as support/1.x
                rect rgb(211, 211, 211)
                    Note over main,support: Version 1.2.3#59; stable<br/>Ready
                end

            """);
    }
}
