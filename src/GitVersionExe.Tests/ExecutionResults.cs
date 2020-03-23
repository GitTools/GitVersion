using System;
using System.Collections.Generic;
using GitVersion.OutputVariables;
using Newtonsoft.Json;

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
                var jsonStartIndex = Output.IndexOf("{", StringComparison.Ordinal);
                var jsonEndIndex = Output.IndexOf("}", StringComparison.Ordinal);
                var json = Output.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);

                var outputVariables = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                return VersionVariables.FromDictionary(outputVariables);
            }
        }
    }
}
