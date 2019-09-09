using System;
using GitVersion.OutputVariables;

namespace GitVersion.BuildServers
{
    public class ContinuaCi : BuildServerBase
    {

        public const string EnvironmentVariableName = "ContinuaCI.Version";

        public override bool CanApplyToCurrentContext()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariableName));

        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                $"@@continua[setVariable name='GitVersion_{name}' value='{value}' skipIfNotDefined='true']"
            };
        }

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            return $"@@continua[setBuildVersion value='{variables.FullSemVer}']";
        }    
    }
}
