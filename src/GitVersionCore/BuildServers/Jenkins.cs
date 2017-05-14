﻿namespace GitVersion
{
    using System;
    using System.IO;

    public class Jenkins : BuildServerBase
    {
        string _file;

        public Jenkins() : this("gitversion.properties")
        {
        }

        public Jenkins(string propertiesFileName)
        {
            _file = propertiesFileName;
        }

        public override bool CanApplyToCurrentContext()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL"));
        }

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            return variables.FullSemVer;
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                string.Format("GitVersion_{0}={1}", name, value)
            };
        }

        public override string GetCurrentBranch(bool usingDynamicRepos)
        {
            return IsPipelineAsCode()
                ? Environment.GetEnvironmentVariable("BRANCH_NAME")
                : (Environment.GetEnvironmentVariable("GIT_LOCAL_BRANCH") ?? Environment.GetEnvironmentVariable("GIT_BRANCH"));
        }

        private bool IsPipelineAsCode()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BRANCH_NAME"));
        }

        public override bool PreventFetch()
        {
            return true;
        }

        /// <summary>
        /// When Jenkins uses pipeline-as-code, it creates two remotes: "origin" and "origin1".
        /// This should be cleaned up, so that normizaling the Git repo will not fail.
        /// </summary>
        /// <returns></returns>
        public override bool ShouldCleanUpRemotes()
        {
            return IsPipelineAsCode();
        }

        public override void WriteIntegration(Action<string> writer, VersionVariables variables)
        {
            base.WriteIntegration(writer, variables);
            writer(string.Format("Outputting variables to '{0}' ... ", _file));
            WriteVariablesFile(variables);
        }

        void WriteVariablesFile(VersionVariables variables)
        {
            File.WriteAllLines(_file, BuildOutputFormatter.GenerateBuildLogOutput(this, variables));
        }
    }
}