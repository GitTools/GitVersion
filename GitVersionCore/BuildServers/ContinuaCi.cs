namespace GitVersion
{
    using Microsoft.Win32;

    public class ContinuaCi : BuildServerBase
    {
        Arguments arguments;

        public ContinuaCi(Arguments arguments)
        {
            this.arguments = arguments;
        }

        public override bool CanApplyToCurrentContext()
        {
            const string KeyName = @"Software\VSoft Technologies\Continua CI Agent";

            if (RegistryKeyExists(KeyName, RegistryView.Registry32))
            {
                return true;
            }

            if (RegistryKeyExists(KeyName, RegistryView.Registry64))
            {
                return true;
            }

            return false;
        }

        public override void PerformPreProcessingSteps(string gitDirectory)
        {
            if (string.IsNullOrEmpty(gitDirectory))
            {
                throw new WarningException("Failed to find .git directory on agent");
            }

            GitHelper.NormalizeGitDirectory(gitDirectory, arguments);
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                string.Format("@@continua[setVariable name='GitVersion_{0}' value='{1}' skipIfNotDefined='true']", name, value)
            };
        }

        public override string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            return string.Format("@@continua[setBuildVersion value='{0}']", versionToUseForBuildNumber);
        }

        static bool RegistryKeyExists(string keyName, RegistryView registryView)
        {
            var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView);
            localKey = localKey.OpenSubKey(keyName);

            return localKey != null;
        }
    }
}
