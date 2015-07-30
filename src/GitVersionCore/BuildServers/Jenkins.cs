namespace GitVersion
{
    using System;

    public class Jenkins : BuildServerBase
    {
        // string _file = "gitversion.properties"

        public override bool CanApplyToCurrentContext()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL__"));
        }

        public override string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            throw new NotImplementedException();
            // tbd
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            throw new NotImplementedException();
            // this returns an array of property lines
        }

        public override void WriteIntegration(Action<string> writer, VersionVariables variables)
        {
            // log message that we're running on jenkins and writing a properties file
            // foreach in GenerateSetParameterMessage write line to file
        }
    }
}