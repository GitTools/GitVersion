using System;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Model.Configuration;
using GitVersionCore.Tests.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class IgnoreBeforeScenarios : TestBase
    {
        [Test]
        public void ShouldFallbackToBaseVersionWhenAllCommitsAreIgnored()
        {
            using var fixture = new EmptyRepositoryFixture();
            var objectId = fixture.Repository.MakeACommit();
            var commit = Substitute.For<ICommit>();
            commit.Sha.Returns(objectId.Sha);
            commit.When.Returns(DateTimeOffset.Now);

            var config = new ConfigurationBuilder()
                .Add(new Config
                {
                    Ignore = new IgnoreConfig
                    {
                        Before = commit.When.AddMinutes(1)
                    }
                }).Build();

            fixture.AssertFullSemver("0.1.0+0", config);
        }
    }
}
