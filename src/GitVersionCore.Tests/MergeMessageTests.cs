using GitVersion;
using NUnit.Framework;
using Shouldly;
using System;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class MergeMessageTests : TestBase
    {
        private readonly Config _config = new Config { TagPrefix = "[vV]" };

        [Test]
        public void NullMessageStringThrows()
        {
            // Act / Assert
            Should.Throw<NullReferenceException>(() => new MergeMessage(null, _config));
        }

        [TestCase("")]
        [TestCase("\t\t  ")]
        public void EmptyMessageString(string message)
        {
            // Act
            var sut = new MergeMessage(message, _config);

            // Assert
            sut.TargetBranch.ShouldBeNull();
            sut.MergedBranch.ShouldBeEmpty();
            sut.IsMergedPullRequest.ShouldBeFalse();
            sut.PullRequestNumber.ShouldBeNull();
            sut.Version.ShouldBeNull();
        }

        [TestCase("")]
        [TestCase("\t\t  ")]
        [TestCase(null)]
        public void EmptyTagPrefix(string prefix)
        {
            // Arrange
            var message = "Updated some code.";
            var config = new Config { TagPrefix = prefix };

            // Act
            var sut = new MergeMessage(message, config);

            // Assert
            sut.TargetBranch.ShouldBeNull();
            sut.MergedBranch.ShouldBeEmpty();
            sut.IsMergedPullRequest.ShouldBeFalse();
            sut.PullRequestNumber.ShouldBeNull();
            sut.Version.ShouldBeNull();
        }

        private static readonly object[] MergeMessages =
        {
            new object[] { "Merge branch 'feature/one'", "feature/one", null, null },
            new object[] { "Merge branch 'origin/feature/one'", "origin/feature/one", null, null },
            new object[] { "Merge tag 'v4.0.0' into master", "v4.0.0", "master", new SemanticVersion(4) },
            new object[] { "Merge tag 'V4.0.0' into master", "V4.0.0", "master", new SemanticVersion(4) },
            new object[] { "Merge branch 'feature/4.1/one'", "feature/4.1/one", null, new SemanticVersion(4, 1) },
            new object[] { "Merge branch 'origin/4.1/feature/one'", "origin/4.1/feature/one", null, new SemanticVersion(4, 1) },
            new object[] { "Merge tag 'v://10.10.10.10' into master", "v://10.10.10.10", "master", null }

        };

        [TestCaseSource(nameof(MergeMessages))]
        public void ParsesMergeMessage(
            string message,
            string expectedMergedBranch,
            string expectedTargetBranch,
            SemanticVersion expectedVersion)
        {
            // Act
            var sut = new MergeMessage(message, _config);

            // Assert
            sut.TargetBranch.ShouldBe(expectedTargetBranch);
            sut.MergedBranch.ShouldBe(expectedMergedBranch);
            sut.IsMergedPullRequest.ShouldBeFalse();
            sut.PullRequestNumber.ShouldBeNull();
            sut.Version.ShouldBe(expectedVersion);
        }

        private static readonly object[] GitHubPullPullMergeMessages =
        {
            new object[] { "Merge pull request #1234 from feature/one", "feature/one", null, null, 1234 },
            new object[] { "Merge pull request #1234 in feature/one", "feature/one", null, null, 1234  },
            new object[] { "Merge pull request #1234 in v4.0.0", "v4.0.0", null, new SemanticVersion(4), 1234  },
            new object[] { "Merge pull request #1234 in V4.0.0", "V4.0.0", null, new SemanticVersion(4), 1234  },
            new object[] { "Merge pull request #1234 from origin/feature/one", "origin/feature/one", null, null, 1234  },
            new object[] { "Merge pull request #1234 in feature/4.1/one", "feature/4.1/one", null, new SemanticVersion(4,1), 1234  },
            new object[] { "Merge pull request #1234 in V://10.10.10.10", "V://10.10.10.10", null, null, 1234 },
            

            //TODO: Investigate successful github merge messages that may be invalid
            // Should an empty PR number be valid?
            new object[] { "Merge pull request # from feature/one", "feature/one", null, null, 0  },
            // The branch name appears to be incorrect
            new object[] { "Merge pull request #1234 from feature/one into dev", "feature/one into dev", "dev", null, 1234  },
        };

        [TestCaseSource(nameof(GitHubPullPullMergeMessages))]
        public void ParsesGitHubPullMergeMessage(
            string message,
            string expectedMergedBranch,
            string expectedTargetBranch,
            SemanticVersion expectedVersion,
            int? expectedPullRequestNumber)
        {
            // Act
            var sut = new MergeMessage(message, _config);

            // Assert
            sut.TargetBranch.ShouldBe(expectedTargetBranch);
            sut.MergedBranch.ShouldBe(expectedMergedBranch);
            sut.IsMergedPullRequest.ShouldBeTrue();
            sut.PullRequestNumber.ShouldBe(expectedPullRequestNumber);
            sut.Version.ShouldBe(expectedVersion);
        }

        private static readonly object[] BitBucketPullMergeMessages =
        {
            new object[] { "Merge pull request #1234 from feature/one from feature/two to dev", "feature/two", null, null, 1234  },
            new object[] { "Merge pull request #1234 in feature/one from feature/two to dev", "feature/two", null, null, 1234 },
            new object[] { "Merge pull request #1234 in v4.0.0 from v4.1.0 to dev", "v4.1.0", null, new SemanticVersion(4,1), 1234  },
            new object[] { "Merge pull request #1234 in V4.0.0 from V4.1.0 to dev", "V4.1.0", null, new SemanticVersion(4,1), 1234  },
            new object[] { "Merge pull request #1234 from origin/feature/one from origin/feature/4.2/two to dev", "origin/feature/4.2/two", null, new SemanticVersion(4,2), 1234  },
            new object[] { "Merge pull request #1234 in feature/4.1/one from feature/4.2/two to dev", "feature/4.2/two", null, new SemanticVersion(4,2), 1234  },
            new object[] { "Merge pull request #1234 in feature/4.1/one from feature/4.2/two to dev into master", "feature/4.2/two", "master", new SemanticVersion(4,2), 1234  },
            new object[] { "Merge pull request #1234 in V4.1.0 from V://10.10.10.10 to dev", "V://10.10.10.10", null, null, 1234 },
            //TODO: Investigate successful bitbucket merge messages that may be invalid
            // Regex has double 'from/in from' section.  Is that correct?
            new object[] { "Merge pull request #1234 in feature/4.1/one from feature/4.2/two to dev", "feature/4.2/two", null, new SemanticVersion(4,2), 1234  },
            new object[] { "Merge pull request #1234 from feature/one from v4.0.0 to master", "v4.0.0", null, new SemanticVersion(4), 1234  },
            // target branch is not resolved from targetbranch group
            new object[] { "Merge pull request #1234 from feature/one from feature/two to master" , "feature/two", null, null, 1234 },
            // Should an empty PR number be valid?
            new object[] { "Merge pull request # in feature/one from feature/two to master" , "feature/two", null, null, 0 }
        };

        [TestCaseSource(nameof(BitBucketPullMergeMessages))]
        public void ParsesBitBucketPullMergeMessage(
            string message,
            string expectedMergedBranch,
            string expectedTargetBranch,
            SemanticVersion expectedVersion,
            int? expectedPullRequestNumber)
        {
            // Act
            var sut = new MergeMessage(message, _config);

            // Assert
            sut.TargetBranch.ShouldBe(expectedTargetBranch);
            sut.MergedBranch.ShouldBe(expectedMergedBranch);
            sut.IsMergedPullRequest.ShouldBeTrue();
            sut.PullRequestNumber.ShouldBe(expectedPullRequestNumber);
            sut.Version.ShouldBe(expectedVersion);
        }

        private static readonly object[] SmartGitMergeMessages =
        {
            new object[] { "Finish feature/one", "feature/one", null, null },
            new object[] { "Finish origin/feature/one", "origin/feature/one", null, null },
            new object[] { "Finish v4.0.0", "v4.0.0", null, new SemanticVersion(4) },
            new object[] { "Finish feature/4.1/one", "feature/4.1/one", null, new SemanticVersion(4, 1) },
            new object[] { "Finish origin/4.1/feature/one", "origin/4.1/feature/one", null, new SemanticVersion(4, 1) },
            new object[] { "Finish V://10.10.10.10", "V://10.10.10.10", null, null },

            //TODO: Investigate successful smart git merge messages that may be invalid
            // The branch name appears to be incorrect
            new object[] { "Finish V4.0.0 into master", "V4.0.0 into master", "master", new SemanticVersion(4) }
        };

        [TestCaseSource(nameof(SmartGitMergeMessages))]
        public void ParsesSmartGitMergeMessage(
           string message,
           string expectedMergedBranch,
           string expectedTargetBranch,
           SemanticVersion expectedVersion)
        {
            // Act
            var sut = new MergeMessage(message, _config);

            // Assert
            sut.TargetBranch.ShouldBe(expectedTargetBranch);
            sut.MergedBranch.ShouldBe(expectedMergedBranch);
            sut.IsMergedPullRequest.ShouldBeFalse();
            sut.PullRequestNumber.ShouldBeNull();
            sut.Version.ShouldBe(expectedVersion);
        }

        private static readonly object[] RemoteTrackingMergeMessages =
        {
            new object[] { "Merge remote-tracking branch 'feature/one' into master", "feature/one", "master", null },
            new object[] { "Merge remote-tracking branch 'origin/feature/one' into dev", "origin/feature/one", "dev", null },
            new object[] { "Merge remote-tracking branch 'v4.0.0' into master", "v4.0.0", "master", new SemanticVersion(4) },
            new object[] { "Merge remote-tracking branch 'V4.0.0' into master", "V4.0.0", "master", new SemanticVersion(4) },
            new object[] { "Merge remote-tracking branch 'feature/4.1/one' into dev", "feature/4.1/one", "dev", new SemanticVersion(4, 1) },
            new object[] { "Merge remote-tracking branch 'origin/4.1/feature/one' into master", "origin/4.1/feature/one", "master", new SemanticVersion(4, 1) },
            new object[] { "Merge remote-tracking branch 'v://10.10.10.10' into master", "v://10.10.10.10", "master", null }
        };

        [TestCaseSource(nameof(RemoteTrackingMergeMessages))]
        public void ParsesRemoteTrackingMergeMessage(
           string message,
           string expectedMergedBranch,
           string expectedTargetBranch,
           SemanticVersion expectedVersion)
        {
            // Act
            var sut = new MergeMessage(message, _config);

            // Assert
            sut.TargetBranch.ShouldBe(expectedTargetBranch);
            sut.MergedBranch.ShouldBe(expectedMergedBranch);
            sut.IsMergedPullRequest.ShouldBeFalse();
            sut.PullRequestNumber.ShouldBeNull();
            sut.Version.ShouldBe(expectedVersion);
        }
    }
}
