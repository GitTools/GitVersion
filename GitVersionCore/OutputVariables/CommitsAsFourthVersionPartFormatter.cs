namespace GitVersion
{
    using System;

    public class CommitsAsFourthVersionPartFormatter : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(SemanticVersion))
                return this;

            return null;
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            var semanticVersion = (SemanticVersion)arg;
            var metaData = new SemanticVersionBuildMetaData(semanticVersion.BuildMetaData)
            {
                CommitsSinceTag = null
            }.ToString();

            return string.Format("{0}.{1}{2}{3}",
                semanticVersion.ToString("j"),
                semanticVersion.BuildMetaData.CommitsSinceTag,
                semanticVersion.PreReleaseTag.HasTag() ? "-" + semanticVersion.PreReleaseTag.ToString(format) : string.Empty,
                !string.IsNullOrEmpty(metaData) ? "+" + metaData : string.Empty);
        }
    }
}