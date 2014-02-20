namespace GitFlowVersion.BuildServers
{
    using GitFlowVersion;

    public class ProcessEnvironment : IBuildServer
    {
        public bool CanApplyToCurrentContext()
        {
            return true;
        }

        public void PerformPreProcessingSteps(string gitDirectory)
        {
            if (string.IsNullOrEmpty(gitDirectory))
            {
                throw new ErrorException("Failed to find .git directory");
            }

            GitHelper.NormalizeGitDirectory(gitDirectory);
        }

        public string GenerateSetParameterMessage(string name, string value)
        {
            var variableName = string.Format("GitFlowVersion.{0}", name);
            System.Environment.SetEnvironmentVariable(variableName, value);
            return string.Format("{0} = '{1}'", variableName, value);
        }

        public string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            System.Environment.SetEnvironmentVariable("GitFlowVersion", versionToUseForBuildNumber);
            return string.Format("{0} = '{1}'", "GitFlowVersion", versionToUseForBuildNumber);
        }
    }
}