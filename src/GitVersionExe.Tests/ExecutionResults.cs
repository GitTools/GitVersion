using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using GitVersion.OutputVariables;

namespace GitVersionExe.Tests
{
    public class ExecutionResults
    {
        public ExecutionResults(int exitCode, string output, string logContents)
        {
            ExitCode = exitCode;
            Output = output;
            Log = logContents;
        }

        public int ExitCode { get; private set; }
        public string Output { get; private set; }
        public string Log { get; private set; }

        public virtual VersionVariables OutputVariables
        {
            get
            {
                var regex = new Regex(@"\{(.|\s)*\}");
                var jsonSerializerOptions = new JsonSerializerOptions();
                jsonSerializerOptions.Converters.Add(new NumberToStringConverter());
                var output = regex.Match(Output).Value;
                if (string.IsNullOrWhiteSpace(output)) return null;
                var outputVariables = JsonSerializer.Deserialize<VersionVariables>(output, jsonSerializerOptions);
                var dict = outputVariables.ToDictionary(variable => variable.Key, variable => variable.Value);
                return VersionVariables.FromDictionary(dict);
            }
        }
    }
}
