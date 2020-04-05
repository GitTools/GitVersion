using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    [TestFixture]
    public class TaggedCommitVersionStrategyTests : TestBase
    {
        [TestCase]
        public void TagWithPreReleaseLabelShouldBeTranslatedToPreReleaseWeight()
        {
            /*todo: Extend the configuration to specify the regex & pre-release weights 
                    for the tags.
            */

            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.BranchTo("develop");
            fixture.Repository.MakeATaggedCommit("v1.0.0-alpha");
            
            var strategy = GetVersionStrategy(fixture.Repository, "develop");
            var baseVersion = strategy.GetVersions().Single();

            baseVersion.SemanticVersion.ToString().ShouldBe("1.0.0-1000");
        }

        private static IVersionStrategy GetVersionStrategy(
            IRepository repository, string branch, Config config = null)
        {
            var sp = BuildServiceProvider(repository, branch, config);
            return sp.GetServiceForType<IVersionStrategy, TaggedCommitVersionStrategy>();
        }
    }
}