namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;

    public class BaseVersion
    {
        public BaseVersion(bool shouldIncrement, SemanticVersion semanticVersion, DateTimeOffset? baseVersionWhenFrom)
        {
            ShouldIncrement = shouldIncrement;
            SemanticVersion = semanticVersion;
            BaseVersionWhenFrom = baseVersionWhenFrom;
        }

        public bool ShouldIncrement { get; private set; }

        public SemanticVersion SemanticVersion { get; private set; }

        /// <summary>
        /// Can be null even if the base version has a version
        /// 
        /// This happens when the base version doesn't have a time it came from
        /// </summary>
        public DateTimeOffset? BaseVersionWhenFrom { get; private set; }
    }
}