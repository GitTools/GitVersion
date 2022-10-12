using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class FeatureBranchScenarios : TestBase
{
    [Test]
    public void ShouldInheritIncrementCorrectlyWithMultiplePossibleParentsAndWeirdlyNamedDevelopBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("development");
        Commands.Checkout(fixture.Repository, "development");

        //Create an initial feature branch
        var feature123 = fixture.Repository.CreateBranch("feature/JIRA-123");
        Commands.Checkout(fixture.Repository, "feature/JIRA-123");
        fixture.Repository.MakeCommits(1);

        //Merge it
        Commands.Checkout(fixture.Repository, "development");
        fixture.Repository.Merge(feature123, Generate.SignatureNow());

        //Create a second feature branch
        fixture.Repository.CreateBranch("feature/JIRA-124");
        Commands.Checkout(fixture.Repository, "feature/JIRA-124");
        fixture.Repository.MakeCommits(1);

        fixture.AssertFullSemver("1.1.0-JIRA-124.1+2");
    }

    [Test]
    public void BranchCreatedAfterFastForwardMergeShouldInheritCorrectly()
    {
        var configuration = new GitVersionConfiguration
        {
            Branches =
            {
                {
                    "unstable",
                    new BranchConfiguration
                    {
                        Increment = IncrementStrategy.Minor,
                        Regex = "unstable",
                        SourceBranches = new HashSet<string>(),
                        IsSourceBranchFor = new HashSet<string> { "feature" }
                    }
                }
            }
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("unstable");
        Commands.Checkout(fixture.Repository, "unstable");

        //Create an initial feature branch
        var feature123 = fixture.Repository.CreateBranch("feature/JIRA-123");
        Commands.Checkout(fixture.Repository, "feature/JIRA-123");
        fixture.Repository.MakeCommits(1);

        //Merge it
        Commands.Checkout(fixture.Repository, "unstable");
        fixture.Repository.Merge(feature123, Generate.SignatureNow());

        //Create a second feature branch
        fixture.Repository.CreateBranch("feature/JIRA-124");
        Commands.Checkout(fixture.Repository, "feature/JIRA-124");
        fixture.Repository.MakeCommits(1);

        fixture.AssertFullSemver("1.1.0-JIRA-124.1+2", configuration);
    }

    [Test]
    public void ShouldNotUseNumberInFeatureBranchAsPreReleaseNumberOffDevelop()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("develop");
        Commands.Checkout(fixture.Repository, "develop");
        fixture.Repository.CreateBranch("feature/JIRA-123");
        Commands.Checkout(fixture.Repository, "feature/JIRA-123");
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.1.0-JIRA-123.1+5");
    }

    [Test]
    public void ShouldNotUseNumberInFeatureBranchAsPreReleaseNumberOffMain()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("feature/JIRA-123");
        Commands.Checkout(fixture.Repository, "feature/JIRA-123");
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.1-JIRA-123.1+5");
    }

    [Test]
    public void TestFeatureBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("feature-test");
        Commands.Checkout(fixture.Repository, "feature-test");
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.1-test.1+5");
    }

    [Test]
    public void TestFeaturesBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("features/test");
        Commands.Checkout(fixture.Repository, "features/test");
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.1-test.1+5");
    }

    [Test]
    public void WhenTwoFeatureBranchPointToTheSameCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch("develop");
        Commands.Checkout(fixture.Repository, "develop");
        fixture.Repository.CreateBranch("feature/feature1");
        Commands.Checkout(fixture.Repository, "feature/feature1");
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch("feature/feature2");
        Commands.Checkout(fixture.Repository, "feature/feature2");

        fixture.AssertFullSemver("0.1.0-feature2.1+2");
    }

    [Test]
    public void ShouldBePossibleToMergeDevelopForALongRunningBranchWhereDevelopAndMainAreEqual()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("v1.0.0");

        fixture.Repository.CreateBranch("develop");
        Commands.Checkout(fixture.Repository, "develop");

        const string branchName = "feature/longrunning";
        fixture.Repository.CreateBranch(branchName);
        Commands.Checkout(fixture.Repository, branchName);
        fixture.Repository.MakeACommit();

        Commands.Checkout(fixture.Repository, "develop");
        fixture.Repository.MakeACommit();

        Commands.Checkout(fixture.Repository, MainBranch);
        fixture.Repository.Merge(fixture.Repository.Branches["develop"], Generate.SignatureNow());
        fixture.Repository.ApplyTag("v1.1.0");

        Commands.Checkout(fixture.Repository, branchName);
        fixture.Repository.Merge(fixture.Repository.Branches["develop"], Generate.SignatureNow());

        var configuration = new GitVersionConfiguration { VersioningMode = VersioningMode.ContinuousDeployment };
        fixture.AssertFullSemver("1.2.0-longrunning.2", configuration);
    }

    [Test]
    public void CanUseBranchNameOffAReleaseBranch()
    {
        var configuration = new GitVersionConfiguration
        {
            Branches =
            {
                { "release", new BranchConfiguration { Tag = "build" } },
                { "feature", new BranchConfiguration { Tag = "useBranchName" } }
            }
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("release/0.3.0");
        fixture.MakeATaggedCommit("v0.3.0-build.1");
        fixture.MakeACommit();
        fixture.BranchTo("feature/PROJ-1");
        fixture.MakeACommit();

        fixture.AssertFullSemver("0.3.0-PROJ-1.1+3", configuration);
    }

    [TestCase("alpha", "JIRA-123", "alpha")]
    [TestCase("useBranchName", "JIRA-123", "JIRA-123")]
    [TestCase("alpha.{BranchName}", "JIRA-123", "alpha.JIRA-123")]
    public void ShouldUseConfiguredTag(string tag, string featureName, string preReleaseTagName)
    {
        var configuration = new GitVersionConfiguration
        {
            Branches =
            {
                { "feature", new BranchConfiguration { Tag = tag } }
            }
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        var featureBranchName = $"feature/{featureName}";
        fixture.Repository.CreateBranch(featureBranchName);
        Commands.Checkout(fixture.Repository, featureBranchName);
        fixture.Repository.MakeCommits(5);

        var expectedFullSemVer = $"1.0.1-{preReleaseTagName}.1+5";
        fixture.AssertFullSemver(expectedFullSemVer, configuration);
    }

    [Test]
    public void BranchCreatedAfterFinishReleaseShouldInheritAndIncrementFromLastMainCommitTag()
    {
        using var fixture = new BaseGitFlowRepositoryFixture("0.1.0");
        //validate current version
        fixture.AssertFullSemver("0.2.0-alpha.1");
        fixture.Repository.CreateBranch("release/0.2.0");
        Commands.Checkout(fixture.Repository, "release/0.2.0");

        //validate release version
        fixture.AssertFullSemver("0.2.0-beta.1+0");

        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("release/0.2.0");
        fixture.Repository.ApplyTag("0.2.0");

        //validate main branch version
        fixture.AssertFullSemver("0.2.0");

        fixture.Checkout("develop");
        fixture.Repository.MergeNoFF("release/0.2.0");
        fixture.Repository.Branches.Remove("release/2.0.0");

        fixture.Repository.MakeACommit();

        //validate develop branch version after merging release 0.2.0 to main and develop (finish release)
        fixture.AssertFullSemver("0.3.0-alpha.1");

        //create a feature branch from develop
        fixture.BranchTo("feature/TEST-1");
        fixture.Repository.MakeACommit();

        //I'm not entirely sure what the + value should be but I know the semvar major/minor/patch should be 0.3.0
        fixture.AssertFullSemver("0.3.0-TEST-1.1+2");
    }

    [Test]
    public void ShouldPickUpVersionFromDevelopAfterReleaseBranchCreated()
    {
        using var fixture = new EmptyRepositoryFixture();
        // Create develop and release branches
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.0.0");
        fixture.MakeACommit();
        fixture.Checkout("develop");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1");

        // create a feature branch from develop and verify the version
        fixture.BranchTo("feature/test");
        fixture.AssertFullSemver("1.1.0-test.1+1");
    }

    [Test]
    public void ShouldPickUpVersionFromDevelopAfterReleaseBranchMergedBack()
    {
        using var fixture = new EmptyRepositoryFixture();
        // Create develop and release branches
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.0.0");
        fixture.MakeACommit();

        // merge release into develop
        fixture.Checkout("develop");
        fixture.MergeNoFF("release/1.0.0");
        fixture.AssertFullSemver("1.1.0-alpha.2");

        // create a feature branch from develop and verify the version
        fixture.BranchTo("feature/test");
        fixture.AssertFullSemver("1.1.0-test.1+2");
    }

    public class WhenMainAsIsDevelop
    {
        [Test]
        public void ShouldPickUpVersionFromMainAfterReleaseBranchCreated()
        {
            var configuration = new GitVersionConfiguration
            {
                Branches = new Dictionary<string, BranchConfiguration>
                {
                    {
                        MainBranch, new BranchConfiguration
                        {
                            TracksReleaseBranches = true,
                            Regex = MainBranch
                        }
                    }
                }
            };

            using var fixture = new EmptyRepositoryFixture();
            // Create release branch
            fixture.MakeACommit();
            fixture.BranchTo("release/1.0.0");
            fixture.MakeACommit();
            fixture.Checkout(MainBranch);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.0.1+1", configuration);

            // create a feature branch from main and verify the version
            fixture.BranchTo("feature/test");
            fixture.AssertFullSemver("1.0.1-test.1+1", configuration);
        }

        [Test]
        public void ShouldPickUpVersionFromMainAfterReleaseBranchMergedBack()
        {
            var configuration = new GitVersionConfiguration
            {
                Branches = new Dictionary<string, BranchConfiguration>
                {
                    {
                        MainBranch, new BranchConfiguration
                        {
                            TracksReleaseBranches = true,
                            Regex = MainBranch
                        }
                    }
                }
            };

            using var fixture = new EmptyRepositoryFixture();
            // Create release branch
            fixture.MakeACommit();
            fixture.BranchTo("release/1.0.0");
            fixture.MakeACommit();

            // merge release into main
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("release/1.0.0");
            fixture.AssertFullSemver("1.0.1+2", configuration);

            // create a feature branch from main and verify the version
            fixture.BranchTo("feature/test");
            fixture.AssertFullSemver("1.0.1-test.1+2", configuration);
        }
    }

    public class WhenFeatureBranchHasNoConfig
    {
        [Test]
        public void ShouldPickUpVersionFromMainAfterReleaseBranchCreated()
        {
            using var fixture = new EmptyRepositoryFixture();
            // Create develop and release branches
            fixture.MakeACommit();
            fixture.BranchTo("develop");
            fixture.MakeACommit();
            fixture.BranchTo("release/1.0.0");
            fixture.MakeACommit();
            fixture.Checkout("develop");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.1.0-alpha.1");

            // create a misnamed feature branch (i.e. it uses the default configuration) from develop and verify the version
            fixture.BranchTo("misnamed");
            fixture.AssertFullSemver("1.1.0-misnamed.1+1");
        }

        [Test]
        public void ShouldPickUpVersionFromDevelopAfterReleaseBranchMergedBack()
        {
            using var fixture = new EmptyRepositoryFixture();
            // Create develop and release branches
            fixture.MakeACommit();
            fixture.BranchTo("develop");
            fixture.MakeACommit();
            fixture.BranchTo("release/1.0.0");
            fixture.MakeACommit();

            // merge release into develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("release/1.0.0");
            fixture.AssertFullSemver("1.1.0-alpha.2");

            // create a misnamed feature branch (i.e. it uses the default configuration) from develop and verify the version
            fixture.BranchTo("misnamed");
            fixture.AssertFullSemver("1.1.0-misnamed.1+2");
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public class WhenMainMarkedAsIsDevelop
        {
            [Test]
            public void ShouldPickUpVersionFromMainAfterReleaseBranchCreated()
            {
                var configuration = new GitVersionConfiguration
                {
                    Branches = new Dictionary<string, BranchConfiguration>
                    {
                        {
                            MainBranch, new BranchConfiguration
                            {
                                TracksReleaseBranches = true,
                                Regex = MainBranch
                            }
                        }
                    }
                };

                using var fixture = new EmptyRepositoryFixture();
                // Create release branch
                fixture.MakeACommit();
                fixture.BranchTo("release/1.0.0");
                fixture.MakeACommit();
                fixture.Checkout(MainBranch);
                fixture.MakeACommit();
                fixture.AssertFullSemver("1.0.1+1", configuration);

                // create a misnamed feature branch (i.e. it uses the default configuration) from main and verify the version
                fixture.BranchTo("misnamed");
                fixture.AssertFullSemver("1.0.1-misnamed.1+1", configuration);
            }

            [Test]
            public void ShouldPickUpVersionFromMainAfterReleaseBranchMergedBack()
            {
                var configuration = new GitVersionConfiguration
                {
                    Branches = new Dictionary<string, BranchConfiguration>
                    {
                        {
                            MainBranch, new BranchConfiguration
                            {
                                TracksReleaseBranches = true,
                                Regex = MainBranch
                            }
                        }
                    }
                };

                using var fixture = new EmptyRepositoryFixture();
                // Create release branch
                fixture.MakeACommit();
                fixture.BranchTo("release/1.0.0");
                fixture.MakeACommit();

                // merge release into main
                fixture.Checkout(MainBranch);
                fixture.MergeNoFF("release/1.0.0");
                fixture.AssertFullSemver("1.0.1+2", configuration);

                // create a misnamed feature branch (i.e. it uses the default configuration) from main and verify the version
                fixture.BranchTo("misnamed");
                fixture.AssertFullSemver("1.0.1-misnamed.1+2", configuration);
            }
        }
    }

    [Test]
    public void PickUpVersionFromMainMarkedWithIsTracksReleaseBranches()
    {
        var configuration = new GitVersionConfiguration
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Branches = new Dictionary<string, BranchConfiguration>
            {
                {
                    MainBranch, new BranchConfiguration
                    {
                        Tag = "pre",
                        TracksReleaseBranches = true
                    }
                },
                {
                    "release", new BranchConfiguration
                    {
                        IsReleaseBranch = true,
                        Tag = "rc"
                    }
                }
            }
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();

        // create a release branch and tag a release
        fixture.BranchTo("release/0.10.0");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.10.0-rc.1+2", configuration);

        // switch to main and verify the version
        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.10.1-pre.1+1", configuration);

        // create a feature branch from main and verify the version
        fixture.BranchTo("MyFeatureD");
        fixture.AssertFullSemver("0.10.1-MyFeatureD.1+1", configuration);
    }

    [Test]
    public void ShouldHaveAGreaterSemVerAfterDevelopIsMergedIntoFeature()
    {
        var configuration = new GitVersionConfiguration
        {
            VersioningMode = VersioningMode.ContinuousDeployment,
            AssemblyVersioningScheme = AssemblyVersioningScheme.Major,
            AssemblyFileVersioningFormat = "{MajorMinorPatch}.{env:WeightedPreReleaseNumber ?? 0}",
            CommitMessageIncrementing = CommitMessageIncrementMode.Disabled,
            Branches = new Dictionary<string, BranchConfiguration>
            {
                {
                    "develop", new BranchConfiguration
                    {
                        PreventIncrementOfMergedBranchVersion = true
                    }
                },
                {
                    "feature", new BranchConfiguration
                    {
                        Tag = "feat-{BranchName}"
                    }
                }
            }
        };
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.ApplyTag("16.23.0");
        fixture.MakeACommit();
        fixture.BranchTo("feature/featX");
        fixture.MakeACommit();
        fixture.Checkout("develop");
        fixture.MakeACommit();
        fixture.Checkout("feature/featX");
        fixture.MergeNoFF("develop");
        fixture.AssertFullSemver("16.24.0-feat-featX.4", configuration);
    }
}
