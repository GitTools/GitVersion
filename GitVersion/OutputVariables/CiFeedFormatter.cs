namespace GitVersion
{
    using System;

    public class CiFeedFormatter : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(SemanticVersion))
                return this;

            return null;
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            var semanticVersion = (SemanticVersion) arg;

            switch (format)
            {
                case "s":
                case "sp":
                case "f":
                case "fp":
                    return string.Format("{0}.{1}{2}", semanticVersion.ToString("j"),
                        semanticVersion.BuildMetaData.CommitsSinceTag ?? 0,
                        semanticVersion.PreReleaseTag.HasTag() ? "-" + semanticVersion.PreReleaseTag.Name: null);
                default:
                    return semanticVersion.ToString(format);
            }
        }
    }
}