using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitVersion.OutputVariables;
using GitVersion.Extensions;

namespace GitVersion.OutputFormatters
{
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
                // preserve leading zeros for padding
                if (int.TryParse(variable.Value, out var value) && NotAPaddedNumber(variable))
                    builder.AppendLineFormat("  \"{0}\":{1}{2}", variable.Key, value, isLast ? string.Empty : ",");
                else
                    builder.AppendLineFormat("  \"{0}\":\"{1}\"{2}", variable.Key, variable.Value, isLast ? string.Empty : ",");
            }

            builder.Append("}");
            return builder.ToString();
        }

        private static bool NotAPaddedNumber(KeyValuePair<string, string> variable)
        {
            if (variable.Value == "0")
                return true;

            return !variable.Value.StartsWith("0");
        }
    }
}
