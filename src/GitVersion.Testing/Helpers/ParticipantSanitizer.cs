using GitVersion.Core;

namespace GitVersion.Testing.Helpers;

public static class ParticipantSanitizer
{
    /// <summary>
    /// Converts a participant identifier to a standardized format that won't break PlantUml.
    /// </summary>
    /// <param name="participant">The participant identifier to convert. This value cannot be null, empty, or consist only of whitespace.</param>
    public static string SanitizeParticipant(string participant)
    {
        GuardAgainstInvalidParticipants(participant);

        return RegexPatterns.Output.SanitizeParticipantRegex.Replace(participant, "_");
    }

    private static void GuardAgainstInvalidParticipants(string participant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(participant);
        if (participant.EndsWith('/'))
        {
            throw new ArgumentException("The value cannot end with a folder separator ('/').", nameof(participant));
        }
    }
}
