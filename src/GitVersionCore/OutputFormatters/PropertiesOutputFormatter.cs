namespace GitVersion
{
    using System.Text;

    public static class PropertiesOutputFormatter
    {
        public static string ToProperties(VersionVariables variables)
        {
            var builder = new StringBuilder();
            foreach (var variable in variables)
            {
                builder.AppendLineFormat("gitversion.{0}={1}", variable.Key, variable.Value);
            }
            return builder.ToString();
        }
    }
}