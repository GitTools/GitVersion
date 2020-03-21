using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class VersionSourceTests : TestBase
    {
        private INextVersionCalculator nextVersionCalculator;
        private IGitVersionContextFactory gitVersionContextFactory;

        [SetUp]
        public void SetUp()
        {
            var config = new Config().ApplyDefaults();
            var options = Options.Create(new Arguments { OverrideConfig = config });

            var sp = ConfigureServices(services =>
            {
                services.AddSingleton(options);
            });

            nextVersionCalculator = sp.GetService<INextVersionCalculator>();
            gitVersionContextFactory = sp.GetService<IGitVersionContextFactory>();
        }

        [Test]
        public void VersionSourceSha()
        {
            using var fixture = new EmptyRepositoryFixture();
            var initialCommit = fixture.Repository.MakeACommit();
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
            _ = fixture.Repository.MakeACommit();
            var featureBranch = fixture.Repository.CreateBranch("feature/foo");
            Commands.Checkout(fixture.Repository, featureBranch);
            _ = fixture.Repository.MakeACommit();

            gitVersionContextFactory.Init(fixture.Repository, fixture.Repository.Head);
            var context = gitVersionContextFactory.Context;

            var version = nextVersionCalculator.FindVersion(context);

            version.BuildMetaData.VersionSourceSha.ShouldBe(initialCommit.Sha);
            version.BuildMetaData.CommitsSinceVersionSource.ShouldBe(2);
        }

        [Test]
        public void VersionSourceShaOneCommit()
        {
            using var fixture = new EmptyRepositoryFixture();
            var initialCommit = fixture.Repository.MakeACommit();

            gitVersionContextFactory.Init(fixture.Repository, fixture.Repository.Head);
            var context = gitVersionContextFactory.Context;

            var version = nextVersionCalculator.FindVersion(context);

            version.BuildMetaData.VersionSourceSha.ShouldBe(initialCommit.Sha);
            version.BuildMetaData.CommitsSinceVersionSource.ShouldBe(0);
        }

        [Test]
        public void VersionSourceShaUsingTag()
        {
            using var fixture = new EmptyRepositoryFixture();
            _ = fixture.Repository.MakeACommit();
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
            var secondCommit = fixture.Repository.MakeACommit();
            _ = fixture.Repository.Tags.Add("1.0", secondCommit);
            var featureBranch = fixture.Repository.CreateBranch("feature/foo");
            Commands.Checkout(fixture.Repository, featureBranch);
            _ = fixture.Repository.MakeACommit();

            gitVersionContextFactory.Init(fixture.Repository, fixture.Repository.Head);
            var context = gitVersionContextFactory.Context;

            var version = nextVersionCalculator.FindVersion(context);

            version.BuildMetaData.VersionSourceSha.ShouldBe(secondCommit.Sha);
            version.BuildMetaData.CommitsSinceVersionSource.ShouldBe(1);
        }
    }
}
