namespace GitVersion
{
    using System;

    public class VsoAgent : BuildServerBase
    {
        public override bool CanApplyToCurrentContext()
        { 
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")); 
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                string.Format("##vso[task.setvariable variable=GitVersion.{0};]{1}", name, value)
            };
        }

        public override string GetCurrentBranch()
        {
            return Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH");
        }

        public override string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            // Note: the VSO agent does not yet support updating the build display number from a variable
            return null;
        }
    }
}
