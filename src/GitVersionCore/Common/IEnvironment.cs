namespace GitVersion
{
    public interface IEnvironment
    {
        string GetEnvironmentVariable(string variableName);
        void SetEnvironmentVariable(string variableName, string value);
    }
}
