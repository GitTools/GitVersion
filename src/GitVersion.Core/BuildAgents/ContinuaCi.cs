using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents
{
    public class ContinuaCi : BuildAgentBase
    {
        public ContinuaCi(IEnvironment environment, ILog log) : base(environment, log)
        {
        }

        public const string EnvironmentVariableName = "ContinuaCI.Version";

        protected override string EnvironmentVariable { get; } = EnvironmentVariableName;

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

        public override bool PreventFetch() => false;
    }
}
