namespace GitVersion.OutputVariables;

public interface IVersionVariableSerializer
{
    string ToJson(GitVersionVariables gitVersionVariables);
    GitVersionVariables FromFile(string filePath);
    void ToFile(GitVersionVariables gitVersionVariables, string filePath);
}
