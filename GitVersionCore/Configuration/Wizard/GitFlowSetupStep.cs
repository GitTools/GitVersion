namespace GitVersion
{
    using System.Collections.Generic;

    public class GitFlowSetupStep : ConfigInitWizardStep
    {
        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config)
        {
            throw new System.NotImplementedException();
        }

        protected override string Prompt
        {
            get { throw new System.NotImplementedException(); }
        }

        protected override string DefaultResult
        {
            get { throw new System.NotImplementedException(); }
        }
    }
}