namespace GitVersion
{
    using System.Linq;
    using System.Text;

    public static class JsonOutputFormatter
    {
        public static string ToJson(VersionVariables variables)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            var last = variables.Last().Key;
            foreach (var variable in variables)
            {
                var isLast = (variable.Key == last);
                int value;
                if (int.TryParse(variable.Value, out value))
                    builder.AppendLineFormat("  \"{0}\":{1}{2}", variable.Key, value, isLast ? string.Empty : ",");
                else
                    builder.AppendLineFormat("  \"{0}\":\"{1}\"{2}", variable.Key, variable.Value, isLast ? string.Empty : ",");
            }

            builder.Append("}");
            return builder.ToString();
        }
    }
}