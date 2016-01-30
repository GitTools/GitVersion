namespace GitVersion
{
    using System.Collections.Generic;
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
                // preserve leading zeros for padding
                if (int.TryParse(variable.Value, out value) && NotAPaddedNumber(variable))
                    builder.AppendLineFormat("  \"{0}\":{1}{2}", variable.Key, value, isLast ? string.Empty : ",");
                else
                    builder.AppendLineFormat("  \"{0}\":\"{1}\"{2}", variable.Key, variable.Value, isLast ? string.Empty : ",");
            }

            builder.Append("}");
            return builder.ToString();
        }

        static bool NotAPaddedNumber(KeyValuePair<string, string> variable)
        {
            if (variable.Value == "0")
                return true;

            return !variable.Value.StartsWith("0");
        }
    }
}