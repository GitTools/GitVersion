using System.IO.Abstractions;
using GitVersion.Configuration;
using SharpYaml;

namespace GitVersion.App.Tests;

[TestFixture]
public class ConfigurationValidationExecutorTests
{
    [Test]
    public void YamlLocationsAreOneBasedAndMessagesAreRedacted()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var file = Path.Combine(Path.GetTempPath(), "candidate.yml");
        fileSystem.Path.GetFullPath(file, Arg.Any<string>()).Returns(file);
        fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);
        fileSystem.File.Exists(file).Returns(true);
        fileSystem.File.ReadAllText(file).Returns("input");
        var console = Substitute.For<IConsole>();
        var executor = new ConfigurationValidationExecutor(fileSystem, console, Substitute.For<IConfigurationFileLocator>(), new ConfigurationResolver(new InvalidYamlSerializer()));
        var options = new GitVersionOptions { ConfigurationInfo = { ConfigurationFile = file, ValidationJson = true } };

        executor.Execute(options).ShouldBe(1);

        var jsonText = (string)console.ReceivedCalls().Single().GetArguments()[0]!;
        using var json = JsonDocument.Parse(jsonText);
        var diagnostic = json.RootElement.GetProperty("diagnostics")[0];
        diagnostic.GetProperty("code").GetString().ShouldBe("GVCONFIG_YAML");
        diagnostic.GetProperty("line").GetInt32().ShouldBe(3);
        diagnostic.GetProperty("column").GetInt32().ShouldBe(4);
        jsonText.ShouldNotContain("secret configuration value");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnreadableExplicitFileReturnsJsonErrorWithoutDiscovery(bool accessDenied)
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var file = Path.Combine(Path.GetTempPath(), "candidate.yml");
        fileSystem.Path.GetFullPath(file, Arg.Any<string>()).Returns(file);
        fileSystem.File.Exists(file).Returns(true);
        fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);
        fileSystem.File.ReadAllText(file).Returns(_ => throw (accessDenied ? new UnauthorizedAccessException("secret") : new IOException("secret")));
        var console = Substitute.For<IConsole>();
        var locator = Substitute.For<IConfigurationFileLocator>();
        var executor = new ConfigurationValidationExecutor(fileSystem, console, locator, new ConfigurationResolver(new ConfigurationSerializer()));
        var options = new GitVersionOptions { ConfigurationInfo = { ConfigurationFile = file, ValidationJson = true } };

        executor.Execute(options).ShouldBe(1);

        var jsonText = (string)console.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IConsole.WriteLine)).GetArguments()[0]!;
        using var json = JsonDocument.Parse(jsonText);
        json.RootElement.GetProperty("valid").GetBoolean().ShouldBeFalse();
        json.RootElement.GetProperty("file").GetString().ShouldBe(file);
        json.RootElement.GetProperty("diagnostics")[0].GetProperty("code").GetString().ShouldBe("GVCONFIG_FILE");
        jsonText.ShouldNotContain("secret");
        locator.ReceivedCalls().ShouldBeEmpty();
        fileSystem.Directory.Received(1).Exists(options.WorkingDirectory);
        fileSystem.Directory.ReceivedCalls().Count().ShouldBe(1);
        fileSystem.File.ReceivedCalls().Count().ShouldBe(2);
    }

    private sealed class InvalidYamlSerializer : IConfigurationSerializer
    {
#pragma warning disable S2325, S1172 // The interface requires instance methods and all parameters; this failure stub deliberately throws without using them.
        T IConfigurationSerializer.Deserialize<T>(string input) => throw new YamlException(new Mark(10, 2, 3), new Mark(11, 2, 4), "secret configuration value");

        string IConfigurationSerializer.Serialize(object graph) => throw new AssertionException("Validation must not serialize configuration output.");

        string IConfigurationSerializer.SerializeProperty(IGitVersionConfiguration configuration, string path) => throw new AssertionException("Validation must not query configuration values.");
#pragma warning restore S2325, S1172
    }
}
