using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.SetConfig
{
    public class ConfigureBranches : ConfigInitWizardStep
    {
        public ConfigureBranches(IConsole console, IFileSystem fileSystem, ILog log) : base(console, fileSystem, log)
        {
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            if (int.TryParse(result, out var parsed))
            {
                if (parsed == 0)
                {
                    steps.Enqueue(new EditConfigStep(Console, FileSystem, Log));
                    return StepResult.Ok();
                }

                try
                {
                    var foundBranch = OrderedBranches(config).ElementAt(parsed - 1);
                    var branchConfig = foundBranch.Value;
                    if (branchConfig == null)
                    {
                        branchConfig = new BranchConfig {Name = foundBranch.Key};
                        config.Branches.Add(foundBranch.Key, branchConfig);
                    }
                    steps.Enqueue(new ConfigureBranch(Console, FileSystem, Log).WithData(foundBranch.Key, branchConfig));
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
" + string.Join("\r\n", OrderedBranches(config).Select((c, i) => $"{i + 1}) {c.Key}"));
        }

        private static IOrderedEnumerable<KeyValuePair<string, BranchConfig>> OrderedBranches(Config config)
        {
            var defaultConfig = new Config();
            ConfigurationUtils.ApplyDefaultsTo(defaultConfig);
            var defaultConfigurationBranches = defaultConfig.Branches
                .Where(k => !config.Branches.ContainsKey(k.Key))
                // Return an empty branch config
                .Select(v => new KeyValuePair<string, BranchConfig>(v.Key, null));
            return config.Branches.Union(defaultConfigurationBranches).OrderBy(b => b.Key);
        }

        protected override string DefaultResult => "0";
    }
}
