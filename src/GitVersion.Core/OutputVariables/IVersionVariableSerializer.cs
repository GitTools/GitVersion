namespace GitVersion.OutputVariables;

/// <summary>Serializes and deserializes <see cref="GitVersionVariables"/> to and from JSON and disk files.</summary>
public interface IVersionVariableSerializer
{
    /// <summary>Serializes <paramref name="gitVersionVariables"/> to a JSON string.</summary>
    string ToJson(GitVersionVariables gitVersionVariables);

    /// <summary>Deserializes a <see cref="GitVersionVariables"/> instance from the JSON file at <paramref name="filePath"/>.</summary>
    GitVersionVariables FromFile(string filePath);

    /// <summary>Writes <paramref name="gitVersionVariables"/> as JSON to the file at <paramref name="filePath"/>.</summary>
    void ToFile(GitVersionVariables gitVersionVariables, string filePath);
}
