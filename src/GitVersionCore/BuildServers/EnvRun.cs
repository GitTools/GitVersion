namespace GitVersion
{
    using System;
    using System.IO;

    public class EnvRun : BuildServerBase
    {
        public override bool CanApplyToCurrentContext()
        {
            string envRunDatabasePath = Environment.GetEnvironmentVariable("ENVRUN_DATABASE");
            if (!string.IsNullOrEmpty(envRunDatabasePath))
            {
                if (!File.Exists(envRunDatabasePath))
                {
                    Logger.WriteError(string.Format("The database file of EnvRun.exe was not found at {0}.", envRunDatabasePath));
                    return false;
                }

                return true;
            }

            return false;
        }

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            return variables.FullSemVer;
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                string.Format("@@envrun[set name='GitVersion_{0}' value='{1}']", name, value)
            };
        }

        public override bool PreventFetch()
        {
            return true;
        }

    }
}