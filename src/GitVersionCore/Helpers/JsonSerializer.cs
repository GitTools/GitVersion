using System.Text;
using GitVersion.Extensions;

namespace GitVersion.Helpers
{
    public static class JsonSerializer
    {
        public static string Serialize(object obj)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            var first = true;
            foreach (var (key, value) in obj.GetProperties())
            {
                if (!first) builder.AppendLine(",");
                else first = false;

                builder.Append($"  \"{key}\":");

                // preserve leading zeros for padding
                if (NotAPaddedNumber(value) && int.TryParse(value, out var number))
                    builder.Append(number);
                else
                    builder.Append($"\"{value}\"");
            }

            builder.AppendLine().Append("}");
            return builder.ToString();
        }

        private static bool NotAPaddedNumber(string value) => value != null && (value == "0" || !value.StartsWith("0"));
    }
}
