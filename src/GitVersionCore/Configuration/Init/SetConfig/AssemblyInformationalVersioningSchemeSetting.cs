namespace GitVersion.Configuration.Init.SetConfig
{
    using System.Collections.Generic;
    using GitVersion.Helpers;
    using Wizard;

    public class AssemblyInformationalVersioningSchemeSetting : ConfigInitWizardStep
    {
        public AssemblyInformationalVersioningSchemeSetting(IConsole console, IFileSystem fileSystem) : base(console, fileSystem)
        {
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(new EditConfigStep(Console, FileSystem));
                    return StepResult.Ok();
                case "1":
                    config.AssemblyInformationalVersioningScheme = AssemblyInformationalVersioningScheme.FullInformationalVersion;
                    steps.Enqueue(new EditConfigStep(Console, FileSystem));
                    return StepResult.Ok();
                case "2":
                    config.AssemblyInformationalVersioningScheme = AssemblyInformationalVersioningScheme.NugetVersion;
                    steps.Enqueue(new EditConfigStep(Console, FileSystem));
                    return StepResult.Ok();
            }

            return StepResult.InvalidResponseSelected();
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            return @"What assembly informational versioning scheme do you want to use:

0) Go Back
1) Full (e.g. 2.8.0-unstable12 Branch:'develop' Sha:'c415f7a3dbadad7b72c8af4b7ae8993d1ef35710')
2) NuGet style (e.g. 1.0.1-unstable0001)";
        }

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}