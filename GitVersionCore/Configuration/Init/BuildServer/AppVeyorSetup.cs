namespace GitVersion.Configuration.Init.BuildServer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using GitVersion.Configuration.Init.Wizard;
    using GitVersion.Helpers;

    class AppVeyorSetup : ConfigInitWizardStep
    {
        protected override StepResult HandleResult(string result, Queue<ConfigInitWizardStep> steps, Config config, string workingDirectory, IFileSystem fileSystem)
        {
            switch (result)
            {
                case "0":
                    steps.Enqueue(new EditConfigStep());
                    return StepResult.Ok();
                case "1":
                    GenerateBasicConfig(workingDirectory, fileSystem);
                    steps.Enqueue(new EditConfigStep());
                    return StepResult.Ok();
                case "2":
                    GenerateNuGetConfig(workingDirectory, fileSystem);
                    steps.Enqueue(new EditConfigStep());
                    return StepResult.Ok();
            }
            return StepResult.InvalidResponseSelected();
        }

        void GenerateBasicConfig(string workingDirectory, IFileSystem fileSystem)
        {
            WriteConfig(workingDirectory, fileSystem, @"install:
  - choco install gitversion.portable -pre

before_build:
 - ps: ./tools/gitversion.exe /l console /output buildserver /updateAssemblyInfo

build:
  project: <your sln file>");
        }

        void GenerateNuGetConfig(string workingDirectory, IFileSystem fileSystem)
        {
            WriteConfig(workingDirectory, fileSystem, @"install:
  - choco install gitversion.portable -pre

before_build:
 - ps: ./tools/gitversion.exe /l console /output buildserver /updateAssemblyInfo

build:
  project: <your sln file>

after_build:
  - cmd: ECHO nuget pack <Project>\<NuSpec>.nuspec -version ""%GitVersion_NuGetVersion%"" -prop ""target=%CONFIGURATION%""
  - cmd: nuget pack <Project>\<NuSpec>.nuspec - version ""%GitVersion_NuGetVersion%"" - prop ""target=%CONFIGURATION%""
  - cmd: appveyor PushArtifact ""<NuSpec>.%GitVersion_NuGetVersion%.nupkg""");
        }

        void WriteConfig(string workingDirectory, IFileSystem fileSystem, string configContents)
        {
            var outputFilename = GetOutputFilename(workingDirectory, fileSystem);
            fileSystem.WriteAllText(outputFilename, configContents);
            Logger.WriteInfo(string.Format("AppVeyor sample config file written to {0}", outputFilename));
        }

        protected override string GetPrompt(Config config, string workingDirectory, IFileSystem fileSystem)
        {
            var prompt = new StringBuilder();
            if (AppVeyorConfigExists(workingDirectory, fileSystem))
            {
                prompt.AppendLine("GitVersion doesn't support modifying existing appveyor config files. We will generate appveyor.gitversion.yml instead");
                prompt.AppendLine();
            }

            prompt.Append(@"What sort of config template would you like generated?

0) Back
1) Generate basic (gitversion + msbuild) configuration
2) Generate with NuGet package publish");

            return prompt.ToString();
        }

        string GetOutputFilename(string workingDirectory, IFileSystem fileSystem)
        {
            if (AppVeyorConfigExists(workingDirectory, fileSystem))
            {
                var count = 0;
                do
                {
                    var path = Path.Combine(workingDirectory, string.Format("appveyor.gitversion{0}.yml", count == 0 ? string.Empty : "." + count));

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

        static bool AppVeyorConfigExists(string workingDirectory, IFileSystem fileSystem)
        {
            return fileSystem.Exists(Path.Combine(workingDirectory, "appveyor.yml"));
        }

        protected override string DefaultResult
        {
            get { return "0"; }
        }
    }
}