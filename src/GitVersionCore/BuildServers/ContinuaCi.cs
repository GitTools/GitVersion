namespace GitVersion
{
#if NETDESKTOP
    using Microsoft.Win32;

    public class ContinuaCi : BuildServerBase
    {
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

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                string.Format("@@continua[setVariable name='GitVersion_{0}' value='{1}' skipIfNotDefined='true']", name, value)
            };
        }

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            return string.Format("@@continua[setBuildVersion value='{0}']", variables.FullSemVer);
        }

        static bool RegistryKeyExists(string keyName, RegistryView registryView)
        {
            var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView);
            localKey = localKey.OpenSubKey(keyName);

            return localKey != null;
        }
    }

#endif

}
