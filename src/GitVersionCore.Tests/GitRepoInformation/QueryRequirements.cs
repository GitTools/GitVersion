using GitTools;
using GitTools.Testing;
using GitVersion;
using GitVersion.GitRepoInformation;
using NUnit.Framework;
using Shouldly;
using System;

[TestFixture]
public class QueryRequirements
{
    [Test]
    public void ListsAllMergeCommits()
    {
        var config = new Config();
        ConfigurationProvider.ApplyDefaultsTo(config);
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.BranchTo("feature/foo");
            fixture.MakeACommit();
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo");

            fixture.Repository.DumpGraph();

            var metadata = Libgit2RepoMetadataProvider.ReadMetadata(
                new GitVersionContext(fixture.Repository, fixture.Repository.FindBranch("master"), config)
            );

            metadata.CurrentBranch.MergeMessages.Count.ShouldBe(1);
        }
    }
}