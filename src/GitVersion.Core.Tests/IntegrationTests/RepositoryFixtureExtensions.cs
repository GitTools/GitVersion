using GitVersion.Extensions;

namespace GitVersion.Core.Tests.IntegrationTests;

internal static class RepositoryFixtureExtensions
{
    public static void MakeACommit(this RepositoryFixtureBase fixture, string commitMsg)
    {
        fixture.Repository.MakeACommit(commitMsg);

        var participant = fixture.SequenceDiagram.GetParticipant(fixture.Repository.Head.FriendlyName);
        if (commitMsg.Length < 40)
        {
            fixture.SequenceDiagram.DiagramBuilder.AppendLineFormat("{0} -> {0}: Commit '{1}'", participant, commitMsg);
        }
        else
        {
            var formattedCommitMsg = string.Join(SysEnv.NewLine, $"Commit '{commitMsg}'".SplitIntoLines(60));
            fixture.SequenceDiagram.NoteOver(formattedCommitMsg, participant);
        }
    }
}
