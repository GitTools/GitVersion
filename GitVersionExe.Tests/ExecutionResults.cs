namespace AcceptanceTests.Helpers
{
    using System.Collections.Generic;
    using System.Web.Script.Serialization;

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

        public virtual Dictionary<string, string> OutputVariables
        {
            get { return new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(Output); }
        }
    }
}
