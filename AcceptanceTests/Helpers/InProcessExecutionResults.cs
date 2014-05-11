namespace AcceptanceTests.Helpers
{
    using System.Collections.Generic;

    public class InProcessExecutionResults : ExecutionResults
    {
        Dictionary<string, string> variables;

        public InProcessExecutionResults(Dictionary<string, string> variables) : base (0, "", "")
        {
            this.variables = variables;
        }

        public override Dictionary<string, string> OutputVariables
        {
            get { return variables; }
        }
    }
}
