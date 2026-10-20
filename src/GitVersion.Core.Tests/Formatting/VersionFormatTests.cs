using GitVersion.Configuration;
using GitVersion.Formatting;
using GitVersion.OutputVariables;

namespace GitVersion.Tests.Formatting;

[TestFixture]
public class VersionFormatTests
{
    private readonly IEnvironment environment = new TestEnvironment();

    [TestCase("{Major}.{Minor:00}.{Patch}", "12.03.4")]
    [TestCase("{Major.Length}", "2")]
    [TestCase("{Length}", "2")]
    [TestCase("{CommitDate:yyyy}", "2025")]
    [TestCase("{BranchName ?? \"no branch\"}", "no branch")]
    [TestCase("{Unknown ?? \"missing\"}", "missing")]
    public void FormatVersionValuesPreservesObjectFormatting(string template, string expected)
    {
        var semanticVersion = new SemanticVersion(12, 3, 4)
        {
            BuildMetaData = new SemanticVersionBuildMetaData
            {
                CommitDate = new DateTimeOffset(2025, 11, 23, 0, 0, 0, TimeSpan.Zero)
            }
        };
        var source = new SemanticVersionFormatValues(semanticVersion,
            GitFlowConfigurationBuilder.New.Build(), 0);

        template.FormatWith(source, this.environment).ShouldBe(expected);
        template.FormatWith((object)source, this.environment).ShouldBe(expected);
    }

    [TestCase("{Major}.{Minor:00}.{Patch}", "12.03.4")]
    [TestCase("{Major.Length}", "2")]
    [TestCase("{BranchName ?? \"no branch\"}", "no branch")]
    [TestCase("{Unknown ?? \"missing\"}", "missing")]
    public void FormatVariablesPreservesObjectFormatting(string template, string expected)
    {
        var source = new TestableGitVersionVariables { Major = "12", Minor = "3", Patch = "4", BranchName = null };

        template.FormatWith(source, this.environment).ShouldBe(expected);
        template.FormatWith((object)source, this.environment).ShouldBe(expected);
    }

    [Test]
    public void FormatMissingVersionMemberPreservesDiagnostic()
    {
        var source = new SemanticVersionFormatValues(new SemanticVersion(), GitFlowConfigurationBuilder.New.Build(), 0);

        var error = Should.Throw<ArgumentException>(() => "{Missing}".FormatWith(source, this.environment));

        error.Message.ShouldBe("'Missing' is not a property or field on type 'SemanticVersionFormatValues'");
    }

    [TestCase(typeof(SemanticVersionFormatValues))]
    [TestCase(typeof(GitVersionVariables))]
    public void VersionFormattingModelsHaveOnlyStringProperties(Type modelType)
    {
        foreach (var property in modelType.GetProperties())
        {
            property.PropertyType.ShouldBe(typeof(string), $"Update the formatter member graph for {modelType.Name}.{property.Name}");
        }
    }
}
