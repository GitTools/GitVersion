using SharpYaml;

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
        result.ShouldContain("  workflow: GitFlow/v1");
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
                             calculation:
                               tag-prefix: custom-
                             """;

        var result = this.migrationService.Migrate(input);

        this.migrationService.Migrate(result).ShouldBe(result);
    }

    [Test]
    public void MigratesConfiguredValuesWithoutAddingDefaults()
    {
        const string input = """
                             workflow: GitHubFlow/v1
                             mode: ContinuousDeployment
                             update-build-number: false
                             branches:
                               main:
                                 increment: Minor
                                 pre-release-weight: 42
                             """;

        var result = this.migrationService.Migrate(input);

        result.ShouldContain("workflow: GitHubFlow/v1");
        result.ShouldContain("mode: ContinuousDeployment");
        result.ShouldContain("update-build-number: false");
        result.ShouldContain("increment: Minor");
        result.ShouldContain("pre-release-weight: 42");
        result.ShouldNotContain("tag-prefix:");
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

    [Test]
    public void RejectsMalformedYaml()
    {
        const string input = "branches: [";

        Should.Throw<YamlException>(() => this.migrationService.Migrate(input));
    }
}
