using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;

namespace GitVersion.Core.Tests.Configuration;

[TestFixture]
public class ConfigurationExtensionsTests : TestBase
{
    [Test]
    public void GetReleaseBranchConfigReturnsAllReleaseBranches()
    {
        var configuration = new GitVersionConfiguration()
        {
            Branches = new Dictionary<string, BranchConfiguration>
            {
                { "foo", new BranchConfiguration() },
                { "bar", new BranchConfiguration { IsReleaseBranch = true } },
                { "baz", new BranchConfiguration { IsReleaseBranch = true } }
            }
        };

        var result = configuration.GetReleaseBranchConfiguration();

        result.Count.ShouldBe(2);
        result.ShouldNotContain(b => b.Key == "foo");
    }

    [TestCase("release/2.0.0",
        "refs/heads/release/2.0.0", "release/2.0.0", "release/2.0.0",
        true, false, false, false, true)]
    [TestCase("upstream/release/2.0.0",
        "refs/heads/upstream/release/2.0.0", "upstream/release/2.0.0", "upstream/release/2.0.0",
        true, false, false, false, false)]
    [TestCase("origin/release/2.0.0",
        "refs/heads/origin/release/2.0.0", "origin/release/2.0.0", "origin/release/2.0.0",
        true, false, false, false, false)]
    [TestCase("refs/remotes/upstream/release/2.0.0",
        "refs/remotes/upstream/release/2.0.0", "upstream/release/2.0.0", "upstream/release/2.0.0",
        false, false, true, false, false)]
    [TestCase("refs/remotes/origin/release/2.0.0",
        "refs/remotes/origin/release/2.0.0", "origin/release/2.0.0", "release/2.0.0",
        false, false, true, false, true)]
    public void EnsureIsReleaseBranchWithReferenceNameWorksAsExpected(string branchName, string expectedCanonical, string expectedFriendly, string expectedWithoutOrigin,
        bool expectedIsLocalBranch, bool expectedIsPullRequest, bool expectedIsRemoteBranch, bool expectedIsTag, bool expectedIsReleaseBranch)
    {
        var configuration = GitFlowConfigurationBuilder.New.Build();

        var actual = ReferenceName.FromBranchName(branchName);
        var isReleaseBranch = configuration.IsReleaseBranch(actual);

        actual.Canonical.ShouldBe(expectedCanonical);
        actual.Friendly.ShouldBe(expectedFriendly);
        actual.WithoutOrigin.ShouldBe(expectedWithoutOrigin);
        actual.IsLocalBranch.ShouldBe(expectedIsLocalBranch);
        actual.IsPullRequest.ShouldBe(expectedIsPullRequest);
        actual.IsRemoteBranch.ShouldBe(expectedIsRemoteBranch);
        actual.IsTag.ShouldBe(expectedIsTag);
        isReleaseBranch.ShouldBe(expectedIsReleaseBranch);
    }
}
