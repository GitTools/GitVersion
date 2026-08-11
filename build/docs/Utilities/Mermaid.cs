using Cake.Common.Tools.DotNet.Test;
using Common.Utilities;
using IOPath = System.IO.Path;

namespace Docs.Utilities;

public static class Mermaid
{
    private const string GeneratedFilePattern = "DocumentationSamplesFor*.mmd";

    private static readonly DirectoryPath SourceDirectory = Paths.Docs.Combine("diagrams");

    private static readonly FilePath DocumentationTestsProject = Paths.Src
        .Combine("GitVersion.Core.Tests")
        .CombineWithFilePath("GitVersion.Core.Tests.csproj");

    private static readonly FilePath ValidationScript = Paths.Docs
        .Combine("scripts")
        .CombineWithFilePath("validate-mermaid.mjs");

    private static readonly FilePath RuntimeSource = Paths.Root
        .Combine("node_modules")
        .Combine("mermaid")
        .Combine("dist")
        .CombineWithFilePath("mermaid.min.js");

    private static readonly FilePath RuntimeOverride = Paths.Docs
        .Combine("theme")
        .Combine("assets")
        .Combine("js")
        .CombineWithFilePath("mermaid.min.js");

    extension(ICakeContext context)
    {
        public void InstallNodeDependencies() =>
            context.RunTool(
                context.IsRunningOnWindows() ? ["npm.cmd", "npm.exe", "npm"] : ["npm"],
                "ci",
                "npm ci"
            );

        public void GenerateMermaidSources(bool check)
        {
            var sourceDirectory = context.MakeAbsolute(SourceDirectory);
            DirectoryPath? temporaryDirectory = null;
            var outputDirectory = sourceDirectory;

            if (check)
            {
                temporaryDirectory = new DirectoryPath(IOPath.Combine(
                    IOPath.GetTempPath(),
                    $"gitversion-mermaid-sources-{Guid.NewGuid():N}"
                ));
                outputDirectory = temporaryDirectory;
            }

            context.EnsureDirectoryExists(outputDirectory);

            try
            {
                context.DotNetTest(DocumentationTestsProject.FullPath, new DotNetTestSettings
                {
                    PathType = DotNetTestPathType.Project,
                    Filter = "FullyQualifiedName~DocumentationSamplesForGitFlow|FullyQualifiedName~DocumentationSamplesForGitHubFlow",
                    WorkingDirectory = context.MakeAbsolute(Paths.Root),
                    EnvironmentVariables = new Dictionary<string, string>
                    {
                        ["MERMAID_OUTPUT_DIRECTORY"] = outputDirectory.FullPath
                    }
                });

                var generatedFiles = GetGeneratedFileNames(outputDirectory);
                if (check)
                {
                    VerifyGeneratedSources(sourceDirectory, outputDirectory, generatedFiles);
                    context.Information($"Verified {generatedFiles.Count} test-generated Mermaid sources.");
                }
                else
                {
                    context.Information($"Generated {generatedFiles.Count} Mermaid sources from tests.");
                }
            }
            finally
            {
                if (temporaryDirectory is not null && Directory.Exists(temporaryDirectory.FullPath))
                {
                    Directory.Delete(temporaryDirectory.FullPath, recursive: true);
                }
            }
        }

        public void ValidateMermaidSyntax()
        {
            var validationScript = context.MakeAbsolute(ValidationScript);
            var arguments = new ProcessArgumentBuilder();
            arguments.AppendQuoted(validationScript.FullPath);

            context.RunTool(
                context.IsRunningOnWindows() ? ["node.exe", "node"] : ["node"],
                arguments,
                "Mermaid syntax validation"
            );
        }

        public void StageMermaidRuntimeForWyam()
        {
            if (!context.FileExists(RuntimeSource))
            {
                throw new CakeException("The Mermaid runtime is missing. Run the InstallNodeDependencies Cake task.");
            }

            context.EnsureDirectoryExists(RuntimeOverride.GetDirectory());
            context.CopyFile(RuntimeSource, RuntimeOverride);
        }

        private void RunTool(IEnumerable<string> executableNames, string arguments, string description) =>
            context.RunTool(executableNames, new ProcessArgumentBuilder().Append(arguments), description);

        private void RunTool(IEnumerable<string> executableNames, ProcessArgumentBuilder arguments, string description)
        {
            var executable = context.Tools.Resolve(executableNames)
                ?? throw new CakeException($"Unable to find {string.Join(" or ", executableNames)} on PATH.");
            var exitCode = context.StartProcess(executable, new ProcessSettings
            {
                Arguments = arguments,
                WorkingDirectory = context.MakeAbsolute(Paths.Root)
            });

            if (exitCode != 0)
            {
                throw new CakeException($"{description} failed with exit code {exitCode}.");
            }
        }
    }

    private static IReadOnlyList<string> GetGeneratedFileNames(DirectoryPath directory) =>
        [.. Directory.GetFiles(directory.FullPath, GeneratedFilePattern)
            .Select(IOPath.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)];

    private static void VerifyGeneratedSources(
        DirectoryPath sourceDirectory,
        DirectoryPath generatedDirectory,
        IReadOnlyList<string> generatedFiles
    )
    {
        var committedFiles = GetGeneratedFileNames(sourceDirectory);
        var missingFiles = generatedFiles.Except(committedFiles, StringComparer.Ordinal).ToArray();
        var obsoleteFiles = committedFiles.Except(generatedFiles, StringComparer.Ordinal).ToArray();

        if (missingFiles.Length > 0 || obsoleteFiles.Length > 0)
        {
            var details = missingFiles.Select(file => $"Missing: {IOPath.Combine(sourceDirectory.FullPath, file)}")
                .Concat(obsoleteFiles.Select(file => $"Obsolete: {IOPath.Combine(sourceDirectory.FullPath, file)}"));
            throw new CakeException(
                $"Test-generated Mermaid source file names are out of date:{Environment.NewLine}" +
                string.Join(Environment.NewLine, details) + Environment.NewLine +
                "Run the GenerateMermaidSources Cake task and commit the results."
            );
        }

        var staleFiles = generatedFiles.Where(file =>
            !StringComparer.Ordinal.Equals(
                NormalizeLineEndings(File.ReadAllText(IOPath.Combine(sourceDirectory.FullPath, file))),
                NormalizeLineEndings(File.ReadAllText(IOPath.Combine(generatedDirectory.FullPath, file)))
            )
        ).ToArray();

        if (staleFiles.Length > 0)
        {
            throw new CakeException(
                $"Test-generated Mermaid sources are stale:{Environment.NewLine}" +
                string.Join(Environment.NewLine, staleFiles.Select(file => IOPath.Combine(sourceDirectory.FullPath, file))) +
                Environment.NewLine + "Run the GenerateMermaidSources Cake task and commit the results."
            );
        }
    }

    private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');
}
