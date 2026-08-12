namespace GitVersion.Tests.IntegrationTests;

internal static class DocumentationDiagramWriter
{
    private const string OutputDirectoryEnvironmentVariable = "MERMAID_OUTPUT_DIRECTORY";

    internal static void Write(SequenceDiagram diagram, string name, bool withPullRequest)
    {
        var outputDirectory = SysEnv.GetEnvironmentVariable(OutputDirectoryEnvironmentVariable);
        if (!withPullRequest || string.IsNullOrWhiteSpace(outputDirectory))
        {
            return;
        }

        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, $"{name}.mmd"), diagram.GetDiagram());
    }
}
