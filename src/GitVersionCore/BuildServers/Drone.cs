namespace GitVersion
{
    using System;

    public class Drone : BuildServerBase
    {
        public override bool CanApplyToCurrentContext()
        {
            return Environment.GetEnvironmentVariable("DRONE")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            return variables.FullSemVer;
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                $"GitVersion_{name}={value}"
            };
        }

        public override string GetCurrentBranch(bool usingDynamicRepos)
        {
            return Environment.GetEnvironmentVariable("DRONE_BRANCH");
        }
    }
}
