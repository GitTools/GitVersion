namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public class MyGet : BuildServerBase
    {
        public override bool CanApplyToCurrentContext()
        {
            var buildRunner = Environment.GetEnvironmentVariable("BuildRunner");

            return !string.IsNullOrEmpty(buildRunner)
                && buildRunner.Equals("MyGet", StringComparerUtils.IgnoreCaseComparison);
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            var messages = new List<string>
            {
                string.Format("##myget[setParameter name='GitVersion.{0}' value='{1}']", name, ServiceMessageEscapeHelper.EscapeValue(value))
            };

            if (string.Equals(name, "LegacySemVerPadded", StringComparerUtils.IgnoreCaseComparison))
            {
                messages.Add(string.Format("##myget[buildNumber '{0}']", ServiceMessageEscapeHelper.EscapeValue(value)));
            }

            return messages.ToArray();
        }

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            return null;
        }
    }
}
