namespace GitVersion.Configuration.Tests;

[TestFixture]
public class WorkflowsTests
{
    private readonly ConfigurationSerializer serializer = new();

    private static readonly object[][] Workflows =
    [
        ["GitFlow/v1", GitFlowConfigurationBuilder.New],
        ["GitHubFlow/v1", GitHubFlowConfigurationBuilder.New],
        ["TrunkBased/preview1", TrunkBasedConfigurationBuilder.New]
    ];

    [Test(Description = "This test is to ensure that the configuration for workflow is up to date")]
    [TestCaseSource(nameof(Workflows))]
    public void CheckWorkflowsAreUpdated(string workflow, IConfigurationBuilder configurationBuilder)
    {
        var configuration = configurationBuilder.Build();

        var serializedConfiguration = serializer.Serialize(configuration);
        var segments = workflow.Split("/");
        var folderName = segments[0];
        var fileName = segments[^1];

        serializedConfiguration.ShouldMatchApproved(builder => builder
            .WithFilenameGenerator((_, _, type, extension) => FilenameGenerator(fileName, type, extension))
            .WithFileExtension("yml")
            .SubFolder($"approved/{folderName}"));
    }

    private static string FilenameGenerator(string fileName, string type, string ext) =>
        type == "approved"
            ? $"{fileName}.{ext}"
            : $"{fileName}.{type}.{ext}";
}
