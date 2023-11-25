using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests.VersionCalculation.Strategies;

[TestFixture]
public class MergeMessageBaseVersionStrategyTests : TestBase
{
    [Test]
    public void ShouldNotAllowIncrementOfVersion()
    {
        // When a branch is merged in you want to start building stable packages of that version
        // So we shouldn't bump the version
        var mockCommit = GitToolsTestingExtensions.CreateMockCommit();
        mockCommit.Message.Returns("Merge branch 'release-0.1.5'");
        mockCommit.Parents.Returns(GetParents(true));

        var mockBranch = GitToolsTestingExtensions.CreateMockBranch(MainBranch, mockCommit);
        var branches = Substitute.For<IBranchCollection>();
        branches.GetEnumerator().Returns(_ => ((IEnumerable<IBranch>)new[]
        {
            mockBranch
        }).GetEnumerator());

        var mockRepository = Substitute.For<IGitRepository>();
        mockRepository.Head.Returns(mockBranch);
        mockRepository.Branches.Returns(branches);
        mockRepository.Commits.Returns(mockBranch.Commits);

        var contextBuilder = new GitVersionContextBuilder().WithRepository(mockRepository);
        contextBuilder.Build();
        contextBuilder.ServicesProvider.ShouldNotBeNull();
        var strategy = contextBuilder.ServicesProvider.GetServiceForType<IVersionStrategy, MergeMessageVersionStrategy>();
        var context = contextBuilder.ServicesProvider.GetRequiredService<Lazy<GitVersionContext>>().Value;

        strategy.ShouldNotBeNull();
        var baseVersion = strategy.GetBaseVersions(context.Configuration.GetEffectiveBranchConfiguration(mockBranch)).Single();

        baseVersion.ShouldIncrement.ShouldBe(false);
    }

    [TestCase("Merge branch 'release-10.10.50'", true, "10.10.50")]
    [TestCase("Merge branch 'release-0.2.0'", true, "0.2.0")]
    [TestCase("Merge branch 'Release-0.2.0'", true, "0.2.0")]
    [TestCase("Merge branch 'Release/0.2.0'", true, "0.2.0")]
    [TestCase("Merge branch 'releases-0.2.0'", true, "0.2.0")]
    [TestCase("Merge branch 'Releases-0.2.0'", true, "0.2.0")]
    [TestCase("Merge branch 'Releases/0.2.0'", true, "0.2.0")]
    [TestCase("Merge branch 'release-4.6.6' into support-4.6", true, "4.6.6")]
    [TestCase("Merge branch 'release-0.1.5'\n\nRelates to: TicketId", true, "0.1.5")]
    [TestCase("Finish Release-0.12.0", true, "0.12.0")] //Support Syntevo SmartGit/Hg's Gitflow merge commit messages for finishing a 'Release' branch
    [TestCase("Merge branch 'Release-v0.2.0'", true, "0.2.0")]
    [TestCase("Merge remote-tracking branch 'origin/release/0.8.0' into develop/" + MainBranch, true, "0.8.0")]
    [TestCase("Merge remote-tracking branch 'refs/remotes/origin/release/2.0.0'", true, "2.0.0")]
    public void TakesVersionFromMergeOfReleaseBranch(string message, bool isMergeCommit, string expectedVersion)
    {
        var parents = GetParents(isMergeCommit);
        AssertMergeMessage(message, expectedVersion, parents);
        AssertMergeMessage(message + " ", expectedVersion, parents);
        AssertMergeMessage(message + "\r ", expectedVersion, parents);
        AssertMergeMessage(message + "\r", expectedVersion, parents);
        AssertMergeMessage(message + "\r\n", expectedVersion, parents);
        AssertMergeMessage(message + "\r\n ", expectedVersion, parents);
        AssertMergeMessage(message + "\n", expectedVersion, parents);
        AssertMergeMessage(message + "\n ", expectedVersion, parents);
    }

    [TestCase("Merge branch 'hotfix-0.1.5'", false)]
    [TestCase("Merge branch 'develop' of github.com:Particular/NServiceBus into develop", true)]
    [TestCase("Merge branch '4.0.3'", true)]
    [TestCase("Merge branch 's'", true)]
    [TestCase("Merge tag '10.10.50'", true)]
    [TestCase("Merge branch 'somebranch' into release-3.0.0", true)]
    [TestCase("Merge branch 'alpha-0.1.5'", true)]
    [TestCase("Merge pull request #95 from Particular/issue-94", false)]
    [TestCase("Merge pull request #95 in Particular/issue-94", true)]
    [TestCase("Merge pull request #95 in Particular/issue-94", false)]
    [TestCase("Merge pull request #64 from arledesma/feature-VS2013_3rd_party_test_framework_support", true)]
    [TestCase("Merge pull request #500 in FOO/bar from Particular/release-1.0.0 to develop)", true)]
    [TestCase("Merge pull request #500 in FOO/bar from feature/new-service to develop)", true)]
    [TestCase("Finish 0.14.1", true)] // Don't support Syntevo SmartGit/Hg's Gitflow merge commit messages for finishing a 'Hotfix' branch
    public void ShouldNotTakeVersionFromMergeOfNonReleaseBranch(string message, bool isMergeCommit)
    {
        var parents = GetParents(isMergeCommit);
        AssertMergeMessage(message, null, parents);
        AssertMergeMessage(message + " ", null, parents);
        AssertMergeMessage(message + "\r ", null, parents);
        AssertMergeMessage(message + "\r", null, parents);
        AssertMergeMessage(message + "\r\n", null, parents);
        AssertMergeMessage(message + "\r\n ", null, parents);
        AssertMergeMessage(message + "\n", null, parents);
        AssertMergeMessage(message + "\n ", null, parents);
    }

    [TestCase("Merge pull request #165 from organization/Particular/release-1.0.0", true)]
    [TestCase("Merge pull request #165 in organization/Particular/release-1.0.0", true)]
    [TestCase("Merge pull request #500 in FOO/bar from organization/Particular/release-1.0.0 to develop)", true)]
    public void ShouldNotTakeVersionFromMergeOfReleaseBranchWithRemoteOtherThanOrigin(string message, bool isMergeCommit)
    {
        var parents = GetParents(isMergeCommit);
        AssertMergeMessage(message, null, parents);
        AssertMergeMessage(message + " ", null, parents);
        AssertMergeMessage(message + "\r ", null, parents);
        AssertMergeMessage(message + "\r", null, parents);
        AssertMergeMessage(message + "\r\n", null, parents);
        AssertMergeMessage(message + "\r\n ", null, parents);
        AssertMergeMessage(message + "\n", null, parents);
        AssertMergeMessage(message + "\n ", null, parents);
    }

    [TestCase(@"Merge pull request #1 in FOO/bar from feature/ISSUE-1 to develop
* commit '38560a7eed06e8d3f3f1aaf091befcdf8bf50fea':
  Updated jQuery to v2.1.3")]
    [TestCase(@"Merge pull request #45 in BRIKKS/brikks from feature/NOX-68 to develop
* commit '38560a7eed06e8d3f3f1aaf091befcdf8bf50fea':
  Another commit message
  Commit message including a IP-number https://10.50.1.1
  A commit message")]
    [TestCase("Merge branch 'release/Sprint_2.0_Holdings_Computed_Balances'")]
    [TestCase("Merge branch 'develop' of http://10.0.6.3/gitblit/r/... into develop")]
    [TestCase("Merge branch " + MainBranch + " of http://172.16.3.10:8082/r/asu_tk/p_sd")]
    [TestCase("Merge branch " + MainBranch + " of http://212.248.89.56:8082/r/asu_tk/p_sd")]
    [TestCase("Merge branch 'DEMO' of http://10.10.10.121/gitlab/mtolland/orcid into DEMO")]
    public void ShouldNotTakeVersionFromUnrelatedMerge(string commitMessage)
    {
        var parents = GetParents(true);

        AssertMergeMessage(commitMessage, null, parents);
    }

    [TestCase("Merge branch 'support/0.2.0'", "support", "0.2.0")]
    [TestCase("Merge branch 'support/0.2.0'", null, null)]
    [TestCase("Merge branch 'release/2.0.0'", null, "2.0.0")]
    public void TakesVersionFromMergeOfConfiguredReleaseBranch(string message, string? releaseBranch, string? expectedVersion)
    {
        var configurationBuilder = GitFlowConfigurationBuilder.New;
        if (releaseBranch != null)
        {
            configurationBuilder.WithBranch(releaseBranch, builder => builder.WithIsReleaseBranch(true));
        }
        ConfigurationHelper configurationHelper = new(configurationBuilder.Build());
        var parents = GetParents(true);

        AssertMergeMessage(message, expectedVersion, parents, configurationHelper.Dictionary);
    }

    private static void AssertMergeMessage(string message, string? expectedVersion, IEnumerable<ICommit?> parents, IReadOnlyDictionary<object, object?>? configuration = null)
    {
        var commit = GitToolsTestingExtensions.CreateMockCommit();
        commit.Message.Returns(message);
        commit.Parents.Returns(parents);

        var mockBranch = GitToolsTestingExtensions.CreateMockBranch(MainBranch, commit, GitToolsTestingExtensions.CreateMockCommit());

        var mockRepository = Substitute.For<IGitRepository>();
        mockRepository.Head.Returns(mockBranch);
        mockRepository.Commits.Returns(mockBranch.Commits);

        var contextBuilder = new GitVersionContextBuilder()
            .WithOverrideConfiguration(configuration)
            .WithRepository(mockRepository);
        contextBuilder.Build();
        contextBuilder.ServicesProvider.ShouldNotBeNull();
        var strategy = contextBuilder.ServicesProvider.GetServiceForType<IVersionStrategy, MergeMessageVersionStrategy>();
        var context = contextBuilder.ServicesProvider.GetRequiredService<Lazy<GitVersionContext>>().Value;

        strategy.ShouldNotBeNull();
        var baseVersion = strategy.GetBaseVersions(context.Configuration.GetEffectiveBranchConfiguration(mockBranch)).SingleOrDefault();

        if (expectedVersion == null)
        {
            baseVersion.ShouldBe(null);
        }
        else
        {
            baseVersion.ShouldNotBeNull();
            baseVersion.GetSemanticVersion().ToString().ShouldBe(expectedVersion);
        }
    }

    private static List<ICommit> GetParents(bool isMergeCommit) =>
        isMergeCommit
            ? new List<ICommit> { new MockCommit(), new MockCommit() }
            : new List<ICommit> { new MockCommit(), };

    private class MockCommit : ICommit
    {
        public bool Equals(ICommit? other) => throw new NotImplementedException();
        public int CompareTo(ICommit? other) => throw new NotImplementedException();
        public bool Equals(IGitObject? other) => throw new NotImplementedException();
        public int CompareTo(IGitObject? other) => throw new NotImplementedException();
        public IObjectId Id => throw new NotImplementedException();
        public string Sha => throw new NotImplementedException();
        public IEnumerable<ICommit> Parents => throw new NotImplementedException();
        public DateTimeOffset When => throw new NotImplementedException();
        public string Message => throw new NotImplementedException();
    }
}
