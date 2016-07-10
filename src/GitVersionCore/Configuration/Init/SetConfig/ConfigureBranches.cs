namespace GitVersion.Configuration.Init.SetConfig
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Wizard;
    using GitVersion.Helpers;

    public class ConfigureBranches : ConfigInitWizardStep
    {
        public ConfigureBranches(IConsole console, IFileSystem fileSystem) : base(console, fileSystem)
        {
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            int parsed;
            if (int.TryParse(result, out parsed))
            {
                if (parsed == 0)
                {
                    steps.Enqueue(new EditConfigStep(Console, FileSystem));
                    return StepResult.Ok();
                }

                try
                {
                    var foundBranch = OrderedBranches(config).ElementAt(parsed - 1);
                    var branchConfig = foundBranch.Value;
                    if (branchConfig == null)
                    {
                        branchConfig = new BranchConfig();
                        config.Branches.Add(foundBranch.Key, branchConfig);
                    }
                    steps.Enqueue(new ConfigureBranch(foundBranch.Key, branchConfig, Console, FileSystem));
                    return StepResult.Ok();
                }
                catch (ArgumentOutOfRangeException)
                { }
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return @"Which branch would you like to configure:

0) Go Back
" + string.Join("\r\n", OrderedBranches(config).Select((c, i) => string.Format("{0}) {1}", i + 1, c.Key)));
        }

        static IOrderedEnumerable<KeyValuePair<string, BranchConfig>> OrderedBranches(Config config)
        {
            var defaultConfig = new Config();
            ConfigurationProvider.ApplyDefaultsTo(defaultConfig);
            var defaultConfigurationBranches = defaultConfig.Branches
                .Where(k => !config.Branches.ContainsKey(k.Key))
                // Return an empty branch config
                .Select(v => new KeyValuePair<string, BranchConfig>(v.Key, null));
            return config.Branches.Union(defaultConfigurationBranches).OrderBy(b => b.Key);
        }

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}