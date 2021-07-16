using System.Collections.Generic;
using System.Text;
using GitTools.Testing.Internal;

namespace GitTools.Testing
{
    /// <summary>
    /// Creates an abstraction over a PlantUML Sequence diagram to draw a sequence diagram of a git repository being created
    /// </summary>
    public class SequenceDiagram
    {
        readonly Dictionary<string, string> _participants = new Dictionary<string, string>();
        readonly StringBuilder _diagramBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SequenceDiagram"/> class.
        /// </summary>
        public SequenceDiagram()
        {
            this._diagramBuilder = new StringBuilder();
            this._diagramBuilder.AppendLine("@startuml");
        }

        /// <summary>
        /// Activates a branch/participant in the sequence diagram
        /// </summary>
        public void Activate(string branch)
        {
            this._diagramBuilder.AppendLineFormat("activate {0}", GetParticipant(branch));
        }

        /// <summary>
        /// Destroys a branch/participant in the sequence diagram
        /// </summary>
        public void Destroy(string branch)
        {
            this._diagramBuilder.AppendLineFormat("destroy {0}", GetParticipant(branch));
        }

        /// <summary>
        /// Creates a participant in the sequence diagram
        /// </summary>
        public void Participant(string participant, string @as = null)
        {
            this._participants.Add(participant, @as ?? participant);
            if (@as == null)
                this._diagramBuilder.AppendLineFormat("participant {0}", participant);
            else
                this._diagramBuilder.AppendLineFormat("participant \"{0}\" as {1}", participant, @as);
        }

        /// <summary>
        /// Appends a divider with specified text to the sequence diagram
        /// </summary>
        public void Divider(string text)
        {
            this._diagramBuilder.AppendLineFormat("== {0} ==", text);
        }

        /// <summary>
        /// Appends a note over one or many participants to the sequence diagram
        /// </summary>
        public void NoteOver(string noteText, string startParticipant, string endParticipant = null, string prefix = null, string color = null)
        {
            this._diagramBuilder.AppendLineFormat(
                prefix + @"note over {0}{1}{2}
  {3}
end note",
                GetParticipant(startParticipant),
                endParticipant == null ? null : ", " + GetParticipant(endParticipant),
                color == null ? null : " " + color,
                noteText.Replace("\n", "\n  "));
        }

        /// <summary>
        /// Appends applying a tag to the specified branch/participant to the sequence diagram
        /// </summary>
        public void ApplyTag(string tag, string toBranch)
        {
            this._diagramBuilder.AppendLineFormat("{0} -> {0}: tag {1}", GetParticipant(toBranch), tag);
        }

        /// <summary>
        /// Appends branching from a branch to another branch, @as can override the participant name
        /// </summary>
        public void BranchTo(string branchName, string currentName, string @as)
        {
            if (!this._participants.ContainsKey(branchName))
            {
                this._diagramBuilder.Append("create ");
                Participant(branchName, @as);
            }

            this._diagramBuilder.AppendLineFormat(
                "{0} -> {1}: branch from {2}",
                GetParticipant(currentName),
                GetParticipant(branchName), currentName);
        }

        /// <summary>
        /// Appends branching from a tag to a specified branch to the sequence diagram
        /// </summary>
        public void BranchToFromTag(string branchName, string fromTag, string onBranch, string @as)
        {
            if (!this._participants.ContainsKey(branchName))
            {
                this._diagramBuilder.Append("create ");
                Participant(branchName, @as);
            }

            this._diagramBuilder.AppendLineFormat("{0} -> {1}: branch from tag ({2})", GetParticipant(onBranch), GetParticipant(branchName), fromTag);
        }

        /// <summary>
        /// Appends a commit on the target participant/branch to the sequence diagram
        /// </summary>
        public void MakeACommit(string toParticipant)
        {
            this._diagramBuilder.AppendLineFormat("{0} -> {0}: commit", GetParticipant(toParticipant));
        }

        /// <summary>
        /// Append a merge to the sequence diagram
        /// </summary>
        public void Merge(string @from, string to)
        {
            this._diagramBuilder.AppendLineFormat("{0} -> {1}: merge", GetParticipant(@from), GetParticipant(to));
        }

        string GetParticipant(string branch)
        {
            if (this._participants.ContainsKey(branch))
                return this._participants[branch];

            return branch;
        }

        /// <summary>
        /// Ends the sequence diagram
        /// </summary>
        public void End()
        {
            this._diagramBuilder.AppendLine("@enduml");
        }

        /// <summary>
        /// returns the plantUML representation of the Sequence Diagram
        /// </summary>
        public string GetDiagram()
        {
            return this._diagramBuilder.ToString();
        }
    }
}
