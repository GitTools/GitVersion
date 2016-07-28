namespace GitVersion
{
    using System;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;

    internal static class ProjectJsonVersionReplacer
    {

        public static ReplacementResult Replace(string json, VersionVariables variables)
        {
            try
            {
                var pos = GetVersionPosition(json);
                if (pos == null)
                    return new ReplacementResult
                    {
                        VersionElementNotFound = true
                    };

                return new ReplacementResult()
                {
                    JsonWithReplacement = ReplaceVersion(json, pos, variables.InformationalVersion)
                };
            }
            catch (Exception ex)
            {
                return new ReplacementResult()
                {
                    Error = ex.Message,
                    HasError = true
                };
            }
        }

        private static string ReplaceVersion(string contents, VersionPosition pos, string version)
        {
            var sb = new StringBuilder();
            using (var reader = new StringReader(contents))
            using (var writer = new StringWriter(sb))
            {
                for (var x = 1; x < pos.LineNumber; x++)
                    writer.WriteLine(reader.ReadLine());

                var str = reader.ReadLine();
                if (str != null)
                {
                    writer.Write(str.Substring(0, pos.LinePosition - pos.Length - 1));
                    writer.Write(version);
                    writer.WriteLine(str.Substring(pos.LinePosition - 1));
                }
                writer.Write(reader.ReadToEnd());
            }
            return sb.ToString();
        }

        private static VersionPosition GetVersionPosition(string contents)
        {
            using (var r = new JsonTextReader(new StringReader(contents)))
                while (r.Read())
                {
                    if (r.Depth == 1 && r.TokenType == JsonToken.PropertyName && (string)r.Value == "version")
                    {
                        r.Read();
                        if (r.TokenType == JsonToken.String)
                            return new VersionPosition(r.LineNumber, r.LinePosition, ((string)r.Value).Length);
                    }
                }

            return null;
        }

        class VersionPosition
        {
            public VersionPosition(int lineNumber, int linePosition, int length)
            {
                LineNumber = lineNumber;
                LinePosition = linePosition;
                Length = length;
            }

            public int LineNumber { get; set; }
            public int LinePosition { get; set; }
            public int Length { get; set; }
        }

        public class ReplacementResult
        {
            public bool VersionElementNotFound { get; set; }
            public string JsonWithReplacement { get; set; }
            public string Error { get; set; }
            public bool HasError { get; set; }
        }
    }
}