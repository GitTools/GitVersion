using System;
using System.IO;
using GitVersion.OutputVariables;
using GitVersion.Helpers;

namespace GitVersion.BuildServers
{
    public class EnvRun : BuildServerBase
    {
        public const string EnvironmentVariableName = "ENVRUN_DATABASE";
        protected override string EnvironmentVariable { get; } = EnvironmentVariableName;
        public override bool CanApplyToCurrentContext()
        {
            string envRunDatabasePath = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (!string.IsNullOrEmpty(envRunDatabasePath))
            {
                if (!File.Exists(envRunDatabasePath))
                {
                    Logger.WriteError($"The database file of EnvRun.exe was not found at {envRunDatabasePath}.");
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
                $"@@envrun[set name='GitVersion_{name}' value='{value}']"
            };
        }
        public override bool PreventFetch() => true;
    }
}
