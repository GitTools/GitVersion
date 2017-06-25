using System;
using GitVersion;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using GitVersion.VersionFilters;
using NUnit.Framework;
using Shouldly;
using GitVersion.GitRepoInformation;

namespace GitVersionCore.Tests.VersionFilters
{
    [TestFixture]
    public class ShaVersionFilterTests
    {
        [Test]
        public void VerifyNullGuard()
        {
            Should.Throw<ArgumentNullException>(() => new ShaVersionFilter(null));
        }

        [Test]
        public void VerifyNullGuard2()
        {
            var commit = new MockCommit();
            var sut = new ShaVersionFilter(new[] { commit.Sha });

            string reason;
            Should.Throw<ArgumentNullException>(() => sut.Exclude(null, new MockRepository(), out reason));
        }

        [Test]
        public void WhenShaMatchShouldExcludeWithReason()
        {
            var commit = new MockCommit();
            var context = new GitVersionContextBuilder().Build();
            var version = new BaseVersion(context, false, new SemanticVersion(1), new BaseVersionSource(new MCommit(commit, 0), "dummy"), string.Empty);
            var sut = new ShaVersionFilter(new[] { commit.Sha });

            string reason;
            sut.Exclude(version, context.Repository, out reason).ShouldBeTrue();
            reason.ShouldNotBeNullOrWhiteSpace();
        }

        [Test]
        public void WhenShaMismatchShouldNotExclude()
        {
            var commit = new MockCommit();
            var context = new GitVersionContextBuilder().Build();
            var version = new BaseVersion(context, false, new SemanticVersion(1), new BaseVersionSource(new MCommit(commit, 0), "dummy"), string.Empty);
            var sut = new ShaVersionFilter(new[] { "mismatched" });

            string reason;
            sut.Exclude(version, context.Repository, out reason).ShouldBeFalse();
            reason.ShouldBeNull();
        }
    }
}
