using System.Collections.Generic;
using GitVersion.OutputVariables;
using GitVersionExe.Tests.Helpers;

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
                var outputVariables = CustomJsonSerializer.Deserialize<Dictionary<string, string>>(Output);
                return VersionVariables.FromDictionary(outputVariables);
            }
        }
    }
}
