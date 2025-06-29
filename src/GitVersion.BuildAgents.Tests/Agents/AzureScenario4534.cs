using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.BuildAgents.Tests.Agents;

internal class AzureScenario4534 : TestBase
{
    private IEnvironment environment;
    private const string ActualMainBranchName = "master";
    private const string ActualDevelopBranchName = "develop";

    [SetUp]
    public void SetUp()
    {
        var sp = ConfigureServices(services => services.AddSingleton<AzurePipelines>());
        environment = sp.GetRequiredService<IEnvironment>();
        environment.SetEnvironmentVariable("TF_BUILD", "true");
        environment.SetEnvironmentVariable("BUILD_SOURCEBRANCH", ActualDevelopBranchName);
        environment.SetEnvironmentVariable("BUILD_BUILDNUMBER", "123456");
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "Readability")]
    public void Scenario4534()
    {
        const string gitVersionYmlFilePath = "GitVersion.BuildAgents.Tests.Agents.4534.yml";
        using (var stream = typeof(AzureScenario4534).Assembly.GetManifestResourceStream(gitVersionYmlFilePath))
        {
            if (stream is null)
                throw new InvalidOperationException($"The configuration file {gitVersionYmlFilePath} was not found in the assembly resources.");

            GitVersionConfiguration? gitVersionConfiguration;
            using (var reader = new StreamReader(stream))
            {
                gitVersionConfiguration = new ConfigurationSerializer()
                    .Deserialize<GitVersionConfiguration?>(reader.ReadToEnd());
            }

            gitVersionConfiguration.ShouldNotBeNull();
            gitVersionConfiguration.DeploymentMode.ShouldBe(DeploymentMode.ContinuousDelivery);
            gitVersionConfiguration.Branches.ShouldNotBeNull();
            gitVersionConfiguration.Branches.ContainsKey(ConfigurationConstants.MainBranchKey).ShouldBeTrue();
            gitVersionConfiguration.Branches.ContainsKey(ConfigurationConstants.DevelopBranchKey).ShouldBeTrue();
            using (var fixture = new EmptyRepositoryFixture(ActualMainBranchName))
            {
                fixture.MakeACommit();
                fixture.BranchTo(ActualDevelopBranchName);
                fixture.AssertFullSemver("0.1.0-alpha.1");
                fixture.GetVersion(gitVersionConfiguration).PreReleaseLabel.ShouldBe(
                    ActualDevelopBranchName,
                    $"What is expected?\nTo get \"{ActualDevelopBranchName}\" as \"PreReleaseLabel\" " +
                    "since the branch name is given via env var BUILD_SOURCEBRANCH.");
                fixture.SequenceDiagram.NoteOver(
                    string.Join(
                        SysEnv.NewLine,
                        ($"PreReleaseLabel should be '{ActualDevelopBranchName}' as the branch name is " +
                        "set via environment variable BUILD_SOURCEBRANCH: and this " +
                        "has been confirmed.").SplitIntoLines(40)),
                    ActualDevelopBranchName);
            }
        }
    }
}
