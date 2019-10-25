using System;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersionCore.Tests.IntegrationTests;
using NUnit.Framework;
using Shouldly;
using GitVersion.Logging;
using GitVersion.Extensions;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class GitRepoMetadataProviderTests : TestBase
    {
        [Test]
        public void FindsCorrectMergeBaseForForwardMerge()
        {
            //*9dfb8b4 49 minutes ago(develop)
            //*54f21b2 53 minutes ago
            //    |\  
            //    | | *a219831 51 minutes ago(HEAD -> release-2.0.0)
            //    | |/
            //    | *4441531 54 minutes ago
            //    | *89840df 56 minutes ago
            //    |/
            //*91bf945 58 minutes ago(master)
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit("initial");
            fixture.BranchTo("develop");
            var expectedReleaseMergeBase = fixture.Repository.Head.Tip;

            // Create release from develop
            fixture.BranchTo("release-2.0.0");

            // Make some commits on release
            fixture.MakeACommit("release 1");
            fixture.MakeACommit("release 2");
            var expectedDevelopMergeBase = fixture.Repository.Head.Tip;

            // First forward merge release to develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("release-2.0.0");

            // Make some new commit on release
            fixture.Checkout("release-2.0.0");
            fixture.MakeACommit("release 3 - after first merge");

            // Make new commit on develop
            fixture.Checkout("develop");

            // Checkout to release (no new commits) 
            fixture.Checkout("release-2.0.0");

            var develop = fixture.Repository.FindBranch("develop");
            var release = fixture.Repository.FindBranch("release-2.0.0");
            var releaseBranchMergeBase = new GitRepoMetadataProvider(fixture.Repository, new NullLog(), new Config())
                .FindMergeBase(release, develop);

            var developMergeBase = new GitRepoMetadataProvider(fixture.Repository, new NullLog(), new Config())
                .FindMergeBase(develop, release);

            fixture.Repository.DumpGraph(Console.WriteLine);

            releaseBranchMergeBase.ShouldBe(expectedReleaseMergeBase);
            developMergeBase.ShouldBe(expectedDevelopMergeBase);
        }

        [Test]
        public void FindsCorrectMergeBaseForForwardMergeMovesOn()
        {
            //*9dfb8b4 49 minutes ago(develop)
            //*54f21b2 53 minutes ago
            //    |\  
            //    | | *a219831 51 minutes ago(HEAD -> release-2.0.0)
            //    | |/
            //    | *4441531 54 minutes ago
            //    | *89840df 56 minutes ago
            //    |/
            //*91bf945 58 minutes ago(master)
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit("initial");
            fixture.BranchTo("develop");
            var expectedReleaseMergeBase = fixture.Repository.Head.Tip;

            // Create release from develop
            fixture.BranchTo("release-2.0.0");

            // Make some commits on release
            fixture.MakeACommit("release 1");
            fixture.MakeACommit("release 2");
            var expectedDevelopMergeBase = fixture.Repository.Head.Tip;

            // First forward merge release to develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("release-2.0.0");

            // Make some new commit on release
            fixture.Checkout("release-2.0.0");
            fixture.MakeACommit("release 3 - after first merge");

            // Make new commit on develop
            fixture.Checkout("develop");
            // Checkout to release (no new commits) 
            fixture.MakeACommit("develop after merge");

            // Checkout to release (no new commits) 
            fixture.Checkout("release-2.0.0");

            var develop = fixture.Repository.FindBranch("develop");
            var release = fixture.Repository.FindBranch("release-2.0.0");
            var releaseBranchMergeBase = new GitRepoMetadataProvider(fixture.Repository, new NullLog(), new Config())
                .FindMergeBase(release, develop);

            var developMergeBase = new GitRepoMetadataProvider(fixture.Repository, new NullLog(), new Config())
                .FindMergeBase(develop, release);

            fixture.Repository.DumpGraph(Console.WriteLine);

            releaseBranchMergeBase.ShouldBe(expectedReleaseMergeBase);
            developMergeBase.ShouldBe(expectedDevelopMergeBase);
        }

        [Test]
        public void FindsCorrectMergeBaseForMultipleForwardMerges()
        {
            //*403b294 44 minutes ago(develop)
            //|\  
            //| *306b243 45 minutes ago(HEAD -> release-2.0.0)
            //| *4cf5969 47 minutes ago
            //| *4814083 51 minutes ago
            //* | cddd3cc 49 minutes ago
            //* | 2b2b52a 53 minutes ago
            //|\ \  
            //| |/
            //| *8113776 54 minutes ago
            //| *3c0235e 56 minutes ago
            //|/
            //*f6f1283 58 minutes ago(master)

            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit("initial");
            fixture.BranchTo("develop");
            var expectedReleaseMergeBase = fixture.Repository.Head.Tip;

            // Create release from develop
            fixture.BranchTo("release-2.0.0");

            // Make some commits on release
            fixture.MakeACommit("release 1");
            fixture.MakeACommit("release 2");

            // First forward merge release to develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("release-2.0.0");

            // Make some new commit on release
            fixture.Checkout("release-2.0.0");
            fixture.Repository.MakeACommit("release 3 - after first merge");

            // Make new commit on develop
            fixture.Checkout("develop");
            // Checkout to release (no new commits) 
            fixture.Checkout("release-2.0.0");
            fixture.Checkout("develop");
            fixture.Repository.MakeACommit("develop after merge");

            // Checkout to release (no new commits) 
            fixture.Checkout("release-2.0.0");

            // Make some new commit on release
            fixture.Repository.MakeACommit("release 4");
            fixture.Repository.MakeACommit("release 5");
            var expectedDevelopMergeBase = fixture.Repository.Head.Tip;

            // Second merge release to develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("release-2.0.0");

            // Checkout to release (no new commits) 
            fixture.Checkout("release-2.0.0");

            var develop = fixture.Repository.FindBranch("develop");
            var release = fixture.Repository.FindBranch("release-2.0.0");
            var releaseBranchMergeBase = new GitRepoMetadataProvider(fixture.Repository, new NullLog(), new Config())
                .FindMergeBase(release, develop);

            var developMergeBase = new GitRepoMetadataProvider(fixture.Repository, new NullLog(), new Config())
                .FindMergeBase(develop, release);

            fixture.Repository.DumpGraph(Console.WriteLine);

            releaseBranchMergeBase.ShouldBe(expectedReleaseMergeBase);
            developMergeBase.ShouldBe(expectedDevelopMergeBase);
        }
    }
}
