using GitVersion.Extensions;

namespace GitVersion.Core.Tests.IntegrationTests;

internal static class RepositoryFixtureExtensions
{
    public static void MakeACommit(this RepositoryFixtureBase fixture, string commitMsg)
    {
        fixture.Repository.MakeACommit(commitMsg);
        var diagramBuilder = (StringBuilder?)typeof(SequenceDiagram)
            .GetField("diagramBuilder", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(fixture.SequenceDiagram);

        var participant = GetParticipant(fixture.Repository.Head.FriendlyName);
        if (participant != null)
        {
            AddTheCommitMessage(fixture, commitMsg, diagramBuilder, participant);
        }

        string? GetParticipant(string participantName) =>
            (string?)typeof(SequenceDiagram).GetMethod("GetParticipant", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(fixture.SequenceDiagram,
                [
                    participantName
                ]);
    }

    private static void AddTheCommitMessage(RepositoryFixtureBase fixture, string commitMsg, StringBuilder? diagramBuilder, string participant)
    {
        if (commitMsg.Length < 40)
        {
            diagramBuilder?.AppendLineFormat("{0} -> {0}: Commit '{1}'", participant, commitMsg);
        }
        else
        {
            var formattedCommitMsg = string.Join(System.Environment.NewLine, $"Commit '{commitMsg}'".SplitIntoLines(60));
            fixture.SequenceDiagram.NoteOver(formattedCommitMsg, participant);
        }
    }
}
