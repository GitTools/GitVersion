namespace GitVersion.Configuration.Tests;

[TestFixture]
public class ConfigurationMigrationServiceTests
{
    private readonly IConfigurationMigrationService migrationService = new ConfigurationMigrationService(new ConfigurationSerializer());

    [Test]
    public void MigratesFlatConfigurationToCalculationAndOutputSections()
    {
        const string input = """
                             workflow: GitFlow/v1
                             tag-prefix: custom-
                             update-build-number: false
                             branches:
                               main:
                                 increment: Major
                                 pre-release-weight: 42
                             """;

        var result = this.migrationService.Migrate(input);

        result.ShouldContain("calculation:");
        result.ShouldContain("workflow: GitFlow/v1");
        result.ShouldNotContain("  workflow:");
        result.ShouldContain("  tag-prefix: custom-");
        result.ShouldContain("      increment: Major");
        result.ShouldContain("output:");
        result.ShouldContain("  update-build-number: false");
        result.ShouldContain("      pre-release-weight: 42");
    }

    [Test]
    public void AcceptsNestedConfigurationAndProducesDeterministicOutput()
    {
        const string input = """
                             output:
                               update-build-number: false
                             workflow: GitHubFlow/v1
                             calculation:
                               tag-prefix: custom-
                             """;

        var result = this.migrationService.Migrate(input);

        result.ShouldContain("workflow: GitHubFlow/v1");
        result.ShouldNotContain("  workflow:");
        this.migrationService.Migrate(result).ShouldBe(result);
    }

    [TestCase("GitFlow/v1")]
    [TestCase("GitHubFlow/v1")]
    public void MigratesWorkflowOnlyConfigurationAtRoot(string workflow)
    {
        var result = this.migrationService.Migrate($"workflow: {workflow}");

        result.ShouldContain($"workflow: {workflow}");
        result.ShouldNotContain("  workflow:");
        result.ShouldNotContain("tag-prefix:");
        this.migrationService.Migrate(result).ShouldBe(result);
    }

    [Test]
    public void MigratesDraftCalculationWorkflowToRootWithoutChangingOverrides()
    {
        const string input = """
                             calculation:
                               workflow: GitHubFlow/v1
                               tag-prefix: custom-
                             output:
                               update-build-number: false
                             """;

        var result = this.migrationService.Migrate(input);

        result.ShouldContain("workflow: GitHubFlow/v1");
        result.ShouldNotContain("  workflow:");
        result.ShouldContain("  tag-prefix: custom-");
        result.ShouldContain("  update-build-number: false");
        this.migrationService.Migrate(result).ShouldBe(result);
    }

    [TestCase("GitHubFlow/v1")]
    [TestCase("GitFlow/v1")]
    public void RejectsDuplicateRootAndCalculationWorkflowsEvenWhenEqual(string nestedWorkflow)
    {
        var input = $"workflow: GitHubFlow/v1\ncalculation:\n  workflow: {nestedWorkflow}";

        var exception = Should.Throw<ConfigurationException>(() => this.migrationService.Migrate(input));

        exception.Message.ShouldContain("both the document root and 'calculation.workflow'");
    }

    [TestCase("output:\n  workflow: GitHubFlow/v1")]
    [TestCase("workflow: GitHubFlow/v1\noutput:\n  workflow: GitHubFlow/v1")]
    public void RejectsOutputWorkflow(string input)
    {
        var exception = Should.Throw<ConfigurationException>(() => this.migrationService.Migrate(input));

        exception.Message.ShouldContain("'output.workflow'");
        exception.Message.ShouldContain("document root");
    }

    [Test]
    public void RejectsMixedConfiguration()
    {
        const string input = """
                             calculation: {}
                             tag-prefix: custom-
                             """;

        Should.Throw<ConfigurationException>(() => this.migrationService.Migrate(input));
    }
}
