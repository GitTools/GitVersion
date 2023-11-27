namespace GitVersion.OutputVariables;

public interface IVersionVariableSerializer
{
    GitVersionVariables FromJson(string json);
    string ToJson(GitVersionVariables gitVersionVariables);
    GitVersionVariables FromFile(string filePath);
    void ToFile(GitVersionVariables gitVersionVariables, string filePath);
}
