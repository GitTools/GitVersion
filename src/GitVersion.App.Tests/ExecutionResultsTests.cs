namespace GitVersion.App.Tests;

public class ExecutionResultsTests
{
    private const string VersionJson = """
        {
          "AssemblySemFileVer": "1.2.3.0",
          "AssemblySemVer": "1.2.3.0",
          "BranchName": "main",
          "BuildMetaData": "0",
          "CommitDate": "2026-11-11",
          "CustomVersion": "1.2.3",
          "EscapedBranchName": "main",
          "FullBuildMetaData": "0",
          "FullSemVer": "1.2.3",
          "InformationalVersion": "1.2.3",
          "Major": "1",
          "MajorMinorPatch": "1.2.3",
          "Minor": "2",
          "Patch": "3",
          "PreReleaseLabelName": "",
          "PreReleaseLabelNameWithDash": "",
          "PreReleaseNumber": "",
          "PreReleaseLabel": "",
          "PreReleaseLabelWithDash": "",
          "SemVer": "1.2.3",
          "Sha": "abc123",
          "ShortSha": "abc123",
          "UncommittedChanges": "0",
          "VersionSourceDistance": "0",
          "VersionSourceIncrement": "None",
          "VersionSourceSemVer": "1.2.3",
          "VersionSourceSha": "abc123",
          "WeightedPreReleaseNumber": "0"
        }
        """;

    [TestCase(false)]
    [TestCase(true)]
    public void OutputVariablesReadsStandardOutputWithoutInterleavedDiagnostics(bool hasCombinedOutput)
    {
        const string diagnostic = "INFO End: Write version variables to cache file";
        var combined = VersionJson.Replace("\"FullSemVer\"", diagnostic + "\n\"FullSemVer\"");
        var result = new ExecutionResults(0, hasCombinedOutput ? combined : null)
        {
            StandardOutput = "##teamcity[buildNumber '1.2.3']\n" + VersionJson,
            StandardError = diagnostic
        };

        var variables = result.OutputVariables.ShouldNotBeNull();
        variables.FullSemVer.ShouldBe("1.2.3");
        variables.Sha.ShouldBe("abc123");
    }

    [Test]
    public void OutputVariablesFallsBackToOutputWhenStandardOutputWasNotCaptured()
    {
        var result = new ExecutionResults(0, VersionJson);

        result.OutputVariables.ShouldNotBeNull().FullSemVer.ShouldBe("1.2.3");
    }

    [TestCase("")]
    [TestCase(" ")]
    public void OutputVariablesDoesNotParseOtherStreamsWhenStandardOutputIsEmpty(string standardOutput)
    {
        var result = new ExecutionResults(0, VersionJson)
        {
            StandardOutput = standardOutput,
            StandardError = VersionJson
        };

        result.OutputVariables.ShouldBeNull();
    }
}
