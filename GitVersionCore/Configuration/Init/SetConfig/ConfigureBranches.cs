namespace GitVersion.Configuration.Init.SetConfig
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GitVersion.Configuration.Init.Wizard;

    public class ConfigureBranches : ConfigInitWizardStep
    {
        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config)
        {
            int parsed;
            if (int.TryParse(result, out parsed))
            {
                if (parsed == 0)
                {
                    steps.Enqueue(new EditConfigStep());
                    return StepResult.Ok();
                }

                try
                {
                    var foundBranch = OrderedBranches(config).ElementAt(parsed - 1);
                    steps.Enqueue(new ConfigureBranch(foundBranch.Key, foundBranch.Value));
                    return StepResult.Ok();
                }
                catch (ArgumentOutOfRangeException)
                { }
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config)
        {
            return @"Which branch would you like to configure:

0) Back
" + string.Join("\r\n", OrderedBranches(config).Select((c, i) => string.Format("{0}) {1}", i + 1, c.Key)));
        }

        static IOrderedEnumerable<KeyValuePair<string, BranchConfig>> OrderedBranches(Config config)
        {
            return config.Branches.OrderBy(b => b.Key);
        }

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}