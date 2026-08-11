using GitVersion.Testing.Helpers;
using GitVersion.Testing.Internal;

namespace GitVersion.Testing;

/// <summary>
/// Creates an abstraction over a Mermaid sequence diagram to draw a sequence diagram of a git repository being created.
/// </summary>
public class SequenceDiagram
{
    private const int IndentationSize = 4;
    private readonly Dictionary<string, string> participants = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="T:SequenceDiagram"/> class.
    /// </summary>
    public SequenceDiagram()
    {
        DiagramBuilder = new StringBuilder();
        DiagramBuilder.AppendLine("sequenceDiagram");
    }

    public StringBuilder DiagramBuilder { get; }

    /// <summary>
    /// Activates a branch/participant in the sequence diagram
    /// </summary>
    public void Activate(string branch) => AppendLineFormat("activate {0}", GetParticipant(branch));

    /// <summary>
    /// Deactivates a branch/participant in the sequence diagram
    /// </summary>
    public void Deactivate(string branch) => AppendLineFormat("deactivate {0}", GetParticipant(branch));

    /// <summary>
    /// Destroys a branch/participant in the sequence diagram.
    /// </summary>
    public void Destroy(string branch, string from)
    {
        var participant = GetParticipant(branch);
        var source = GetParticipant(from);

        AppendLineFormat("destroy {0}", participant);
        AppendLineFormat("{0}--x{1}: delete branch", source, participant);
    }

    /// <summary>
    /// Creates a participant in the sequence diagram
    /// </summary>
    public void Participant(string participant, string? @as = null) => Participant(participant, @as, prefix: null);

    private void Participant(string participant, string? @as, string? prefix)
    {
        var cleanParticipant = ParticipantSanitizer.SanitizeParticipant(@as ?? participant);
        this.participants.Add(participant, cleanParticipant);
        if (participant == cleanParticipant)
        {
            AppendLineFormat("{0}participant {1}", prefix, participant);
        }
        else
        {
            AppendLineFormat("{0}participant {1} as {2}", prefix, cleanParticipant, EscapeText(participant));
        }
    }

    /// <summary>
    /// Appends a note over one or many participants to the sequence diagram
    /// </summary>
    public void NoteOver(string noteText, string startParticipant, string? endParticipant = null, string? color = null)
    {
        if (color is not null)
        {
            AppendLineFormat("rect {0}", ToMermaidColor(color));
        }

        AppendLineFormat(
            color is null ? 1 : 2,
            "Note over {0}{1}: {2}",
            GetParticipant(startParticipant),
            endParticipant == null ? null : "," + GetParticipant(endParticipant),
            EscapeText(noteText));

        if (color is not null)
        {
            AppendLine("end");
        }
    }

    /// <summary>
    /// Appends applying a tag to the specified branch/participant to the sequence diagram
    /// </summary>
    public void ApplyTag(string tag, string toBranch) =>
        AppendLineFormat("{0}->>{0}: tag {1}", GetParticipant(toBranch), EscapeText(tag));

    /// <summary>
    /// Appends branching from a branch to another branch, @as can override the participant name
    /// </summary>
    public void BranchTo(string branchName, string currentName, string? @as)
    {
        if (!this.participants.ContainsKey(branchName))
        {
            Participant(branchName, @as, "create ");
        }

        AppendLineFormat(
            "{0}->>{1}: branch from {2}",
            GetParticipant(currentName),
            GetParticipant(branchName), EscapeText(currentName));
    }

    /// <summary>
    /// Appends branching from a tag to a specified branch to the sequence diagram
    /// </summary>
    public void BranchToFromTag(string branchName, string fromTag, string onBranch, string? @as)
    {
        if (!this.participants.ContainsKey(branchName))
        {
            Participant(branchName, @as, "create ");
        }

        AppendLineFormat(
            "{0}->>{1}: branch from tag ({2})",
            GetParticipant(onBranch),
            GetParticipant(branchName),
            EscapeText(fromTag));
    }

    /// <summary>
    /// Appends a commit on the target participant/branch to the sequence diagram
    /// </summary>
    public void MakeACommit(string toParticipant) => AppendLineFormat("{0}->>{0}: commit", GetParticipant(toParticipant));

    public void MakeACommit(string toParticipant, string commitMessage) =>
        AppendLineFormat("{0}->>{0}: Commit '{1}'", GetParticipant(toParticipant), EscapeText(commitMessage));

    /// <summary>
    /// Append a merge to the sequence diagram
    /// </summary>
    public void Merge(string from, string to) => AppendLineFormat("{0}->>{1}: merge", GetParticipant(from), GetParticipant(to));

    public string GetParticipant(string branch) => this.participants.GetValueOrDefault(branch, branch);

    /// <summary>
    /// Ends the sequence diagram. Mermaid sequence diagrams do not require a closing directive.
    /// </summary>
    public void End()
    {
    }

    /// <summary>
    /// Returns the Mermaid representation of the sequence diagram.
    /// </summary>
    public string GetDiagram() => DiagramBuilder.ToString().ReplaceLineEndings("\n");

    private void AppendLine(string text, int indentationLevel = 1)
    {
        DiagramBuilder.Append(' ', IndentationSize * indentationLevel);
        DiagramBuilder.AppendLine(text);
    }

    private void AppendLineFormat(string format, params object?[] args) => AppendLineFormat(1, format, args);

    private void AppendLineFormat(int indentationLevel, string format, params object?[] args)
    {
        DiagramBuilder.Append(' ', IndentationSize * indentationLevel);
        DiagramBuilder.AppendLineFormat(format, args);
    }

    private static string EscapeText(string text) => text
        .Replace("\r\n", "<br/>")
        .Replace("\r", "<br/>")
        .Replace("\n", "<br/>")
        .Replace(";", "#59;");

    private static string ToMermaidColor(string color)
    {
        if (color is ['#', _, _, _, _, _, _]
            && int.TryParse(color.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out var red)
            && int.TryParse(color.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out var green)
            && int.TryParse(color.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out var blue))
        {
            return $"rgb({red}, {green}, {blue})";
        }

        return color;
    }
}
