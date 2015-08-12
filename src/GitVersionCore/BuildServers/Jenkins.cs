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

        public override string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            return versionToUseForBuildNumber;
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                string.Format("GitVersion_{0}={1}", name, value)
            };
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