namespace GitVersion
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

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

        public override string GetCurrentBranch(bool usingDynamicRepos)
        {
            return Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH");
        }

        public override bool PreventFetch()
        {
            return true;
        }

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            // For VSO, we'll get the Build Number and insert GitVersion variables where
            // specified
            var buildNum = Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER");

            var newBuildNum = variables.Aggregate(buildNum, (current, kvp) =>
                current.RegexReplace(string.Format(@"\$\(GITVERSION_{0}\)", kvp.Key), kvp.Value ?? string.Empty, RegexOptions.IgnoreCase));

            // If no variable substitution has happened, use FullSemVer
            if (buildNum == newBuildNum)
            {
                var buildNumber = variables.FullSemVer.EndsWith("+0") ? variables.FullSemVer.Substring(0, variables.FullSemVer.Length - 2) : variables.FullSemVer;
                return string.Format("##vso[build.updatebuildnumber]{0}", buildNumber);
            }

            return string.Format("##vso[build.updatebuildnumber]{0}", newBuildNum);
        }
    }
}
