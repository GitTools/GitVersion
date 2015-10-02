﻿using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class SwitchingToGitFlowScenarios
{
    [Test]
    public void WhenDevelopBranchedFromMasterWithLegacyVersionTags_DevelopCanUseReachableTag()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeCommits(5);
            fixture.Repository.MakeATaggedCommit("1.0.0.0");
            fixture.Repository.MakeCommits(2);
            fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
            fixture.AssertFullSemver("1.1.0-unstable.2");
        }
    }
}