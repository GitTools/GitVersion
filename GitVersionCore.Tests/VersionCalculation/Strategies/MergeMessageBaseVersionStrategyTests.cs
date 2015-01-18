namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    using System.Collections.Generic;
    using GitVersion.VersionCalculation.BaseVersionCalculators;
    using LibGit2Sharp;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class MergeMessageBaseVersionStrategyTests
    {
        [Test]
        public void ShouldNotAllowIncrementOfVersion()
        {
            // When a branch is merged in you want to start building stable packages of that version
            // So we shouldn't bump the version
            var context = new GitVersionContextBuilder().WithRepository(new MockRepository
            {
                Head = new MockBranch("master") { new MockCommit
                {
                    MessageEx = "Merge branch 'hotfix-0.1.5'",
                    ParentsEx = GetParents(true)
                } }
            }).Build();
            var sut = new MergeMessageBaseVersionStrategy();

            var baseVersion = sut.GetVersion(context);

            baseVersion.ShouldIncrement.ShouldBe(false);
        }

        [TestCase("Merge branch 'hotfix-0.1.5'", false, null)]
        [TestCase("Merge branch 'develop' of github.com:Particular/NServiceBus into develop", true, null)]
        [TestCase("Merge branch '4.0.3'", true, "4.0.3")] //TODO: possible make it a config option to support this
        [TestCase("Merge branch 'release-10.10.50'", true, "10.10.50")]
        [TestCase("Merge branch 's'", true, null)] // Must start with a number
        [TestCase("Merge branch 'release-0.2.0'", true, "0.2.0")]
        [TestCase("Merge branch 'hotfix-4.6.6' into support-4.6", true, "4.6.6")]
        [TestCase("Merge branch 'hotfix-10.10.50'", true, "10.10.50")]
        [TestCase("Merge branch 'hotfix-0.1.5'", true, "0.1.5")]
        [TestCase("Merge branch 'hotfix-0.1.5'\n\nRelates to: TicketId", true, "0.1.5")]
        [TestCase("Merge branch 'alpha-0.1.5'", true, "0.1.5")]
        [TestCase("Merge pull request #165 from Particular/release-1.0.0", true, "1.0.0")]
        [TestCase("Merge pull request #95 from Particular/issue-94", false, null)]
        [TestCase("Merge pull request #165 in Particular/release-1.0.0", true, "1.0.0")]
        [TestCase("Merge pull request #95 in Particular/issue-94", true, null)]
        [TestCase("Merge pull request #95 in Particular/issue-94", false, null)]
        [TestCase("Merge pull request #64 from arledesma/feature-VS2013_3rd_party_test_framework_support", true, null)]
        [TestCase("Finish Release-0.12.0", true, "0.12.0")] //Support Syntevo SmartGit/Hg's Gitflow merge commit messages for finishing a 'Release' branch
        [TestCase("Finish 0.14.1", true, "0.14.1")] //Support Syntevo SmartGit/Hg's Gitflow merge commit messages for finishing a 'Hotfix' branch
        public void AssertMergeMessage(string message, bool isMergeCommit, string expectedVersion)
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

        static void AssertMergeMessage(string message, string expectedVersion, List<Commit> parents)
        {
            var commit = new MockCommit
            {
                MessageEx = message,
                ParentsEx = parents
            };

            var context = new GitVersionContextBuilder()
                .WithRepository(new MockRepository
                {
                    Head = new MockBranch("master")
                    {
                        commit,
                        new MockCommit()
                    }
                })
                .Build();
            var sut = new MergeMessageBaseVersionStrategy();

            var baseVersion = sut.GetVersion(context);

            if (expectedVersion == null)
            {
                baseVersion.ShouldBe(null);
            }
            else
            {
                baseVersion.SemanticVersion.ToString().ShouldBe(expectedVersion);
            }
        }

        static List<Commit> GetParents(bool isMergeCommit)
        {
            if (isMergeCommit)
            {
                return new List<Commit>
            {
                null,
                null
            };
            }
            return new List<Commit>
        {
            null
        };
        }
    }
}