namespace GitVersion
{
    using System;

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
                string.Format("@@continua[setVariable name='GitVersion_{0}' value='{1}' skipIfNotDefined='true']", name, value)
            };
        }

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            return string.Format("@@continua[setBuildVersion value='{0}']", variables.FullSemVer);
        }    
    }
}
