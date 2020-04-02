using System.Linq;
using GitTools.Testing;
using GitVersionCore.Tests.Helpers;
using GitVersionTask.Tests.Helpers;
using LibGit2Sharp;

namespace GitVersion.MSBuildTask.Tests
{
    public class TestTaskBase : TestBase
    {
        protected static EmptyRepositoryFixture CreateLocalRepositoryFixture()
        {
            var fixture = new EmptyRepositoryFixture();
            fixture.MakeATaggedCommit("1.2.3");
            fixture.MakeACommit();
            return fixture;
        }

        protected static RemoteRepositoryFixture CreateRemoteRepositoryFixture()
        {
            var fixture = new RemoteRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("develop");

            Commands.Fetch((Repository) fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, new string[0], new FetchOptions(), null);
            Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.Repository.Head.Tip);
            fixture.LocalRepositoryFixture.Repository.Branches.Remove("master");
            fixture.InitializeRepo();
            return fixture;
        }
    }
}
