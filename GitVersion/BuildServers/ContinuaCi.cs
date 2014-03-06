namespace GitVersion
{
    using Microsoft.Win32;

    public class ContinuaCi : IBuildServer
    {
        public bool CanApplyToCurrentContext()
        {
            using (var registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\VSoft Technologies\\Continua CI Agent"))
            {
                return registryKey != null;
            }
        }

        public void PerformPreProcessingSteps(string gitDirectory)
        {
            if (string.IsNullOrEmpty(gitDirectory))
            {
                throw new ErrorException("Failed to find .git directory on agent");
            }

            GitHelper.NormalizeGitDirectory(gitDirectory);
        }

        public string[] GenerateSetParameterMessage(string name, string value)
        {
            return new []
            {
                string.Format("@@continua[setVariable name='GitVersion.{0}' value='{1}']", name, value)
            };
        }

        public string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            return string.Format("@@continua[setBuildVersion value='{0}']", versionToUseForBuildNumber);
        }
    }

}
