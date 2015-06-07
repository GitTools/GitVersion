namespace GitVersion.Configuration.Wizard.SetConfig
{
    using System.Collections.Generic;

    public class GlobalModeSetting : ConfigInitWizardStep
    {
        readonly ConfigInitWizardStep returnToStep;
        readonly bool isPartOfWizard;

        public GlobalModeSetting(ConfigInitWizardStep returnToStep, bool isPartOfWizard)
        {
            this.returnToStep = returnToStep;
            this.isPartOfWizard = isPartOfWizard;
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config)
        {
            switch (result)
            {
                case "1":
                    config.VersioningMode = VersioningMode.ContinuousDelivery;
                    steps.Enqueue(returnToStep);
                    return StepResult.Ok();
                case "2":
                    config.VersioningMode = VersioningMode.ContinuousDeployment;
                    steps.Enqueue(returnToStep);
                    return StepResult.Ok();
                case "0":
                case "3":
                    steps.Enqueue(returnToStep);
                    return StepResult.Ok();
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config)
        {
            return string.Format(@"What do you want the default increment mode to be (can be overriden per branch):
{0}
1) Follow SemVer and only increment when a release has been tagged (continuous delivery mode)
2) Increment based on branch config every commit (continuous deployment mode)
{1}", 
!isPartOfWizard ? "0) Back" : string.Empty,
isPartOfWizard ? "3) Skip" : string.Empty);
        }

        protected override string DefaultResult
        {
            get { return "3"; }
        }
    }
}
