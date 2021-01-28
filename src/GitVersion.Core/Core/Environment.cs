namespace GitVersion
{
    public class Environment : IEnvironment
    {
        public string GetEnvironmentVariable(string variableName)
        {
            return System.Environment.GetEnvironmentVariable(variableName);
        }

        public void SetEnvironmentVariable(string variableName, string value)
        {
            System.Environment.SetEnvironmentVariable(variableName, value);
        }
    }
}
