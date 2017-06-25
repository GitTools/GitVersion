using GitVersion;
using GitVersion.GitRepoInformation;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using GitVersion.VersionFilters;
using NUnit.Framework;
using Shouldly;
using System;

namespace GitVersionCore.Tests.VersionFilters
{
    [TestFixture]
    public class MinDateVersionFilterTests
    {
        [Test]
        public void VerifyNullGuard()
        {
            var dummy = DateTimeOffset.UtcNow.AddSeconds(1.0);
            var sut = new MinDateVersionFilter(dummy);

            string reason;
            Should.Throw<ArgumentNullException>(() => sut.Exclude(null, new MockRepository(), out reason));
        }

        [Test]
        public void WhenCommitShouldExcludeWithReason()
        {
            var context = new GitVersionContextBuilder().Build();
            var source = new BaseVersionSource(new MCommit(new MockCommit(), 0), "dummy");
            var version = new BaseVersion(context, false, new SemanticVersion(1), source, string.Empty);
            var futureDate = DateTimeOffset.UtcNow.AddYears(1);
            var sut = new MinDateVersionFilter(futureDate);

            string reason;
            sut.Exclude(version, context.Repository, out reason).ShouldBeTrue();
            reason.ShouldNotBeNullOrWhiteSpace();
        }

        [Test]
        public void WhenShaMismatchShouldNotExclude()
        {
            var source = new BaseVersionSource(new MCommit(new MockCommit(), 0), "dummy");
            var context = new GitVersionContextBuilder().Build();
            var version = new BaseVersion(context, false, new SemanticVersion(1), source, string.Empty);
            var pastDate = DateTimeOffset.UtcNow.AddYears(-1);
            var sut = new MinDateVersionFilter(pastDate);

            string reason;
            sut.Exclude(version, context.Repository, out reason).ShouldBeFalse();
            reason.ShouldBeNull();
        }
    }
}
