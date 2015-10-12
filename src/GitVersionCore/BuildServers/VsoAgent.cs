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
            return string.Format("##vso[build.updatebuildnumber]{0}", ServiceMessageEscapeHelper.EscapeValue(versionToUseForBuildNumber));
        }
    }
}
