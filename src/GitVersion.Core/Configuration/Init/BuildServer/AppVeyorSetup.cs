using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.Configuration.Init.BuildServer
{
    internal enum ProjectVisibility
    {
        Public = 0,
        Private = 1
    }

    internal class AppVeyorSetup : ConfigInitWizardStep
    {
        private ProjectVisibility projectVisibility;

        public AppVeyorSetup(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory) : base(console, fileSystem, log, stepFactory)
        {
        }

        public AppVeyorSetup WithData(ProjectVisibility visibility)
        {
            projectVisibility = visibility;
            return this;
        }

        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory)
        {
            var editConfigStep = StepFactory.CreateStep<EditConfigStep>();
            switch (result)
            {
                case "0":
                    steps.Enqueue(editConfigStep);
                    return StepResult.Ok();
                case "1":
                    GenerateBasicConfig(workingDirectory);
                    steps.Enqueue(editConfigStep);
                    return StepResult.Ok();
                case "2":
                    GenerateNuGetConfig(workingDirectory);
                    steps.Enqueue(editConfigStep);
                    return StepResult.Ok();
            }
            return StepResult.InvalidResponseSelected();
        }

        private static string GetGvCommand(ProjectVisibility visibility)
        {
            return visibility switch
            {
                ProjectVisibility.Public => "  - ps: gitversion /l console /output buildserver /updateAssemblyInfo",
                ProjectVisibility.Private => "  - ps: gitversion $env:APPVEYOR_BUILD_FOLDER /l console /output buildserver /updateAssemblyInfo /nofetch /b $env:APPVEYOR_REPO_BRANCH",
                _ => ""
            };
        }

        private void GenerateBasicConfig(string workingDirectory)
        {
            WriteConfig(workingDirectory, FileSystem, $@"install:
  - choco install gitversion.portable -pre -y

before_build:
  - nuget restore
{GetGvCommand(projectVisibility)}

build:
  project: <your sln file>");
        }

        private void GenerateNuGetConfig(string workingDirectory)
        {
            WriteConfig(workingDirectory, FileSystem, $@"install:
  - choco install gitversion.portable -pre -y

assembly_info:
  patch: false

before_build:
  - nuget restore
{GetGvCommand(projectVisibility)}

build:
  project: <your sln file>

after_build:
  - cmd: ECHO nuget pack <Project>\<NuSpec>.nuspec -version ""%GitVersion_NuGetVersion%"" -prop ""target=%CONFIGURATION%""
  - cmd: nuget pack <Project>\<NuSpec>.nuspec -version ""%GitVersion_NuGetVersion%"" -prop ""target=%CONFIGURATION%""
  - cmd: appveyor PushArtifact ""<NuSpec>.%GitVersion_NuGetVersion%.nupkg""");
        }

        private void WriteConfig(string workingDirectory, IFileSystem fileSystem, string configContents)
        {
            var outputFilename = GetOutputFilename(workingDirectory, fileSystem);
            fileSystem.WriteAllText(outputFilename, configContents);
            Log.Info($"AppVeyor sample config file written to {outputFilename}");
        }

        protected override string GetPrompt(Config config, string workingDirectory)
        {
            var prompt = new StringBuilder();
            if (AppVeyorConfigExists(workingDirectory, FileSystem))
            {
                prompt.AppendLine("GitVersion doesn't support modifying existing appveyor config files. We will generate appveyor.gitversion.yml instead");
                prompt.AppendLine();
            }

            prompt.Append(@"What sort of config template would you like generated?

0) Go Back
1) Generate basic (gitversion + msbuild) configuration
2) Generate with NuGet package publish");

            return prompt.ToString();
        }

        private string GetOutputFilename(string workingDirectory, IFileSystem fileSystem)
        {
            if (AppVeyorConfigExists(workingDirectory, fileSystem))
            {
                var count = 0;
                do
                {
                    var path = Path.Combine(workingDirectory, $"appveyor.gitversion{(count == 0 ? string.Empty : "." + count)}.yml");

                    if (!fileSystem.Exists(path))
                    {
                        return path;
                    }

                    count++;
                } while (count < 10);
                throw new Exception("appveyor.gitversion.yml -> appveyor.gitversion.9.yml all exist. Pretty sure you have enough templates");
            }

            return Path.Combine(workingDirectory, "appveyor.yml");
        }

        private static bool AppVeyorConfigExists(string workingDirectory, IFileSystem fileSystem)
        {
            return fileSystem.Exists(Path.Combine(workingDirectory, "appveyor.yml"));
        }

        protected override string DefaultResult => "0";
    }
}
