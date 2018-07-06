namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    using System.Collections.Generic;
    using System.Linq;
    using GitVersion.VersionCalculation.BaseVersionCalculators;
    using LibGit2Sharp;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class MergeMessageBaseVersionStrategyTests : TestBase
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

            var baseVersion = sut.GetVersions(context).Single();

            baseVersion.ShouldIncrement.ShouldBe(false);
        }

        [TestCase("Merge branch 'hotfix-0.1.5'", false, null)]
        [TestCase("Merge branch 'develop' of github.com:Particular/NServiceBus into develop", true, null)]
        [TestCase("Merge branch '4.0.3'", true, "4.0.3")] //TODO: possible make it a config option to support this
        [TestCase("Merge branch 'release-10.10.50'", true, "10.10.50")]
        [TestCase("Merge branch 's'", true, null)] // Must start with a number
        [TestCase("Merge tag '10.10.50'", true, "10.10.50")]
        [TestCase("Merge branch 'release-0.2.0'", true, "0.2.0")]
        [TestCase("Merge branch 'Release-0.2.0'", true, "0.2.0")]
        [TestCase("Merge branch 'Release/0.2.0'", true, "0.2.0")]
        [TestCase("Merge branch 'hotfix-4.6.6' into support-4.6", true, "4.6.6")]
        [TestCase("Merge branch 'hotfix-10.10.50'", true, "10.10.50")]
        [TestCase("Merge branch 'Hotfix-10.10.50'", true, "10.10.50")]
        [TestCase("Merge branch 'Hotfix/10.10.50'", true, "10.10.50")]
        [TestCase("Merge branch 'hotfix-0.1.5'", true, "0.1.5")]
        [TestCase("Merge branch 'hotfix-4.2.2' into support-4.2", true, "4.2.2")]
        [TestCase("Merge branch 'somebranch' into release-3.0.0", true, null)]
        [TestCase("Merge branch 'hotfix-0.1.5'\n\nRelates to: TicketId", true, "0.1.5")]
        [TestCase("Merge branch 'alpha-0.1.5'", true, "0.1.5")]
        [TestCase("Merge pull request #165 from Particular/release-1.0.0", true, "1.0.0")]
        [TestCase("Merge pull request #95 from Particular/issue-94", false, null)]
        [TestCase("Merge pull request #165 in Particular/release-1.0.0", true, "1.0.0")]
        [TestCase("Merge pull request #95 in Particular/issue-94", true, null)]
        [TestCase("Merge pull request #95 in Particular/issue-94", false, null)]
        [TestCase("Merge pull request #64 from arledesma/feature-VS2013_3rd_party_test_framework_support", true, null)]
        [TestCase("Merge pull request #500 in FOO/bar from Particular/release-1.0.0 to develop)", true, "1.0.0")]
        [TestCase("Merge pull request #500 in FOO/bar from feature/new-service to develop)", true, null)]
        [TestCase("Finish Release-0.12.0", true, "0.12.0")] //Support Syntevo SmartGit/Hg's Gitflow merge commit messages for finishing a 'Release' branch
        [TestCase("Finish 0.14.1", true, "0.14.1")] //Support Syntevo SmartGit/Hg's Gitflow merge commit messages for finishing a 'Hotfix' branch
        [TestCase("Merge branch 'Release-v0.2.0'", true, "0.2.0")]
        [TestCase("Merge branch 'Release-v2.2'", true, "2.2.0")]
        [TestCase("Merge remote-tracking branch 'origin/release/0.8.0' into develop/master", true, "0.8.0")]
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

        [TestCase(@"Merge pull request #1 in FOO/bar from feature/ISSUE-1 to develop

* commit '38560a7eed06e8d3f3f1aaf091befcdf8bf50fea':
  Updated jQuery to v2.1.3")]
        [TestCase(@"Merge pull request #45 in BRIKKS/brikks from feature/NOX-68 to develop

* commit '38560a7eed06e8d3f3f1aaf091befcdf8bf50fea':
  Another commit message
  Commit message including a IP-number https://10.50.1.1
  A commit message")]
        [TestCase(@"Merge branch 'release/Sprint_2.0_Holdings_Computed_Balances'")]
        [TestCase(@"Merge branch 'develop' of http://10.0.6.3/gitblit/r/... into develop")]
        [TestCase(@"Merge branch 'master' of http://172.16.3.10:8082/r/asu_tk/p_sd")]
        [TestCase(@"Merge branch 'master' of http://212.248.89.56:8082/r/asu_tk/p_sd")]
        [TestCase(@"Merge branch 'DEMO' of http://10.10.10.121/gitlab/mtolland/orcid into DEMO")]
        public void MergeMessagesThatIsNotRelatedToGitVersion(string commitMessage)
        {
            var parents = GetParents(true);

            AssertMergeMessage(commitMessage, null, parents);
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

            var baseVersion = sut.GetVersions(context).SingleOrDefault();

            if (expectedVersion == null)
            {
                baseVersion.ShouldBe(null);
            }
            else
            {
                baseVersion.ShouldNotBeNull();
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