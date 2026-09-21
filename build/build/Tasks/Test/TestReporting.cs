namespace Build.Tasks;

internal static class TestReporting
{
    extension(ProcessArgumentBuilder args)
    {
        internal ProcessArgumentBuilder AppendArguments(DirectoryPath resultsDirectory) => args
            .Append("--report-spekt-junit")
            .Append("--report-spekt-junit-filename").AppendQuoted(resultsDirectory.CombineWithFilePath("results.xml").FullPath)
            .Append("--results-directory").AppendQuoted(resultsDirectory.FullPath)
            .Append("--coverlet")
            .Append("--coverlet-output-format").AppendQuoted("cobertura")
            .Append("--coverlet-exclude").AppendQuoted("[GitVersion*.Tests]*")
            .Append("--coverlet-exclude").AppendQuoted("[GitVersion.Testing]*")
            // The JUnit extension is loaded by the MTP controller and cannot be rewritten on Windows.
            .Append("--coverlet-exclude").AppendQuoted("[Spekt.TestLogger]*");

        internal ProcessArgumentBuilder AppendGitHubArguments() => args
            .Append("--report-gh")
            .Append("--report-gh-annotations on")
            .Append("--report-gh-groups off")
            .Append("--report-gh-step-summary on")
            .Append("--report-gh-step-summary-sections test-results")
            .Append("--report-gh-failure-details on")
            .Append("--report-gh-slow-test-notices off");
    }
}
