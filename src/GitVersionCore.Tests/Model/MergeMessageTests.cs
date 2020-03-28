using System;
using System.Collections.Generic;
using GitVersion;
using GitVersion.Model.Configuration;
using GitVersionCore.Tests.Helpers;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class MergeMessageTests : TestBase
    {
        private readonly Config config = new Config { TagPrefix = "[vV]" };

        [Test]
        public void NullMessageStringThrows()
        {
            // Act / Assert
            Should.Throw<NullReferenceException>(() => new MergeMessage(null, config));
        }

        [TestCase("")]
        [TestCase("\t\t  ")]
        public void EmptyMessageString(string message)
        {
            // Act
            var sut = new MergeMessage(message, config);

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
            var sut = new MergeMessage(message, config);

            // Assert
            sut.FormatName.ShouldBe("Default");
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
            new object[] { "Merge pull request #1234 from origin/feature/one", "origin/feature/one", null, null, 1234  },
            new object[] { "Merge pull request #1234 in feature/4.1/one", "feature/4.1/one", null, new SemanticVersion(4,1), 1234  },
            new object[] { "Merge pull request #1234 in V://10.10.10.10", "V://10.10.10.10", null, null, 1234 },
            new object[] { "Merge pull request #1234 from feature/one into dev", "feature/one", "dev", null, 1234  }
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
            var sut = new MergeMessage(message, config);

            // Assert
            sut.FormatName.ShouldBe("GitHubPull");
            sut.TargetBranch.ShouldBe(expectedTargetBranch);
            sut.MergedBranch.ShouldBe(expectedMergedBranch);
            sut.IsMergedPullRequest.ShouldBeTrue();
            sut.PullRequestNumber.ShouldBe(expectedPullRequestNumber);
            sut.Version.ShouldBe(expectedVersion);
        }

        private static readonly object[] BitBucketPullMergeMessages =
        {
            new object[] { "Merge pull request #1234 from feature/one from feature/two to dev", "feature/two", "dev", null, 1234  },
            new object[] { "Merge pull request #1234 in feature/one from feature/two to dev", "feature/two", "dev", null, 1234 },
            new object[] { "Merge pull request #1234 in v4.0.0 from v4.1.0 to dev", "v4.1.0", "dev", new SemanticVersion(4,1), 1234  },
            new object[] { "Merge pull request #1234 from origin/feature/one from origin/feature/4.2/two to dev", "origin/feature/4.2/two", "dev", new SemanticVersion(4,2), 1234  },
            new object[] { "Merge pull request #1234 in feature/4.1/one from feature/4.2/two to dev", "feature/4.2/two", "dev", new SemanticVersion(4,2), 1234  },
            new object[] { "Merge pull request #1234 from feature/one from feature/two to master" , "feature/two", "master", null, 1234 },
            new object[] { "Merge pull request #1234 in V4.1.0 from V://10.10.10.10 to dev", "V://10.10.10.10", "dev", null, 1234 },
            //TODO: Investigate successful bitbucket merge messages that may be invalid
            // Regex has double 'from/in from' section.  Is that correct?
            new object[] { "Merge pull request #1234 from feature/one from v4.0.0 to master", "v4.0.0", "master", new SemanticVersion(4), 1234  }
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
            var sut = new MergeMessage(message, config);

            // Assert
            sut.FormatName.ShouldBe("BitBucketPull");
            sut.TargetBranch.ShouldBe(expectedTargetBranch);
            sut.MergedBranch.ShouldBe(expectedMergedBranch);
            sut.IsMergedPullRequest.ShouldBeTrue();
            sut.PullRequestNumber.ShouldBe(expectedPullRequestNumber);
            sut.Version.ShouldBe(expectedVersion);
        }

        private static readonly object[] BitBucketPullMergeMessagesv7 =
        {
            new object[] { @"Pull request #68: Release/2.2

Merge in aaa/777 from release/2.2 to master

* commit '750aa37753dec1a85b22cc16db851187649d9e97':", "release/2.2", "master", new SemanticVersion(2,2), 68  }
        };

        [TestCaseSource(nameof(BitBucketPullMergeMessagesv7))]
        public void ParsesBitBucketPullMergeMessagev7(
            string message,
            string expectedMergedBranch,
            string expectedTargetBranch,
            SemanticVersion expectedVersion,
            int? expectedPullRequestNumber)
        {
            // Act
            var sut = new MergeMessage(message, config);

            // Assert
            sut.FormatName.ShouldBe("BitBucketPullv7");
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
            new object[] { "Finish V4.0.0 into master", "V4.0.0", "master", new SemanticVersion(4) }
         };

        [TestCaseSource(nameof(SmartGitMergeMessages))]
        public void ParsesSmartGitMergeMessage(
            string message,
            string expectedMergedBranch,
            string expectedTargetBranch,
            SemanticVersion expectedVersion)
        {
            // Act
            var sut = new MergeMessage(message, config);

            // Assert
            sut.FormatName.ShouldBe("SmartGit");
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
            var sut = new MergeMessage(message, config);

            // Assert
            sut.FormatName.ShouldBe("RemoteTracking");
            sut.TargetBranch.ShouldBe(expectedTargetBranch);
            sut.MergedBranch.ShouldBe(expectedMergedBranch);
            sut.IsMergedPullRequest.ShouldBeFalse();
            sut.PullRequestNumber.ShouldBeNull();
            sut.Version.ShouldBe(expectedVersion);
        }

        private static readonly object[] InvalidMergeMessages =
        {
            new object[] { "Merge pull request # from feature/one", "", null, null, null },
            new object[] { "Merge pull request # in feature/one from feature/two to master" , "", null, null, null },
            new object[] { "Zusammengeführter PR : feature/one mit master mergen", "", null, null, null }
        };

        [TestCaseSource(nameof(InvalidMergeMessages))]
        public void ParsesInvalidMergeMessage(
            string message,
            string expectedMergedBranch,
            string expectedTargetBranch,
            SemanticVersion expectedVersion,
            int? expectedPullRequestNumber)
        {
            // Act
            var sut = new MergeMessage(message, config);

            // Assert
            sut.FormatName.ShouldBeNull();
            sut.TargetBranch.ShouldBe(expectedTargetBranch);
            sut.MergedBranch.ShouldBe(expectedMergedBranch);
            sut.IsMergedPullRequest.ShouldBeFalse();
            sut.PullRequestNumber.ShouldBe(expectedPullRequestNumber);
            sut.Version.ShouldBe(expectedVersion);
        }

        [Test]
        public void MatchesSingleCustomMessage()
        {
            // Arrange
            var message = "My custom message";
            var definition = "Mycustom";
            config.MergeMessageFormats = new Dictionary<string, string>
            {
                [definition] = message
            };

            // Act
            var sut = new MergeMessage(message, config);

            // Assert
            sut.FormatName.ShouldBe(definition);
            sut.TargetBranch.ShouldBeNull();
            sut.MergedBranch.ShouldBeEmpty();
            sut.IsMergedPullRequest.ShouldBeFalse();
            sut.PullRequestNumber.ShouldBeNull();
            sut.Version.ShouldBeNull();
        }

        [Test]
        public void MatchesMultipleCustomMessages()
        {
            // Arrange
            var format = "My custom message";
            var definition = "Mycustom";
            config.MergeMessageFormats = new Dictionary<string, string>
            {
                ["Default2"] = "some example",
                ["Default3"] = "another example",
                [definition] = format
            };

            // Act
            var sut = new MergeMessage(format, config);

            // Assert
            sut.FormatName.ShouldBe(definition);
            sut.TargetBranch.ShouldBeNull();
            sut.MergedBranch.ShouldBeEmpty();
            sut.IsMergedPullRequest.ShouldBeFalse();
            sut.PullRequestNumber.ShouldBeNull();
            sut.Version.ShouldBeNull();
        }

        [Test]
        public void MatchesCaptureGroupsFromCustomMessages()
        {
            // Arrange
            var format = @"^Merged PR #(?<PullRequestNumber>\d+) into (?<TargetBranch>[^\s]*) from (?:(?<SourceBranch>[^\s]*))";
            var definition = "Mycustom";
            config.MergeMessageFormats = new Dictionary<string, string>
            {
                [definition] = format
            };
            var pr = 1234;
            var target = "master";
            var source = "feature/2.0/example";


            // Act
            var sut = new MergeMessage($"Merged PR #{pr} into {target} from {source}", config);

            // Assert
            sut.FormatName.ShouldBe(definition);
            sut.TargetBranch.ShouldBe(target);
            sut.MergedBranch.ShouldBe(source);
            sut.IsMergedPullRequest.ShouldBeTrue();
            sut.PullRequestNumber.ShouldBe(pr);
            sut.Version.ShouldBe(new SemanticVersion(2));
        }

        [Test]
        public void ReturnsAfterFirstMatchingPattern()
        {
            // Arrange
            var format = @"^Merge (branch|tag) '(?<SourceBranch>[^']*)'(?: into (?<TargetBranch>[^\s]*))*";
            var definition = "Mycustom";
            config.MergeMessageFormats = new Dictionary<string, string>
            {
                [definition] = format,
                ["Default2"] = format,
                ["Default3"] = format
            };

            // Act
            var sut = new MergeMessage("Merge branch 'this'", config);

            // Assert
            sut.FormatName.ShouldBe(definition);
            sut.TargetBranch.ShouldBeNull();
            sut.MergedBranch.ShouldBe("this");
            sut.IsMergedPullRequest.ShouldBeFalse();
            sut.PullRequestNumber.ShouldBeNull();
            sut.Version.ShouldBeNull();
        }
    }
}
