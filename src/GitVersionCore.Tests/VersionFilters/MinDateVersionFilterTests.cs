using GitVersion.VersionCalculation.BaseVersionCalculators;
using GitVersion.VersionFilters;
using NUnit.Framework;
using Shouldly;
using System;
using GitVersion;
using GitVersionCore.Tests.Mocks;

namespace GitVersionCore.Tests.VersionFilters
{
    [TestFixture]
    public class MinDateVersionFilterTests : TestBase
    {
        [Test]
        public void VerifyNullGuard()
        {
            var dummy = DateTimeOffset.UtcNow.AddSeconds(1.0);
            var sut = new MinDateVersionFilter(dummy);

            Should.Throw<ArgumentNullException>(() => sut.Exclude(null, out _));
        }

        [Test]
        public void WhenCommitShouldExcludeWithReason()
        {
            var context = new GitVersionContextBuilder().Build();
            var commit = new MockCommit(); //when = UtcNow
            var version = new BaseVersion(context, "dummy", false, new SemanticVersion(1), commit, string.Empty);
            var futureDate = DateTimeOffset.UtcNow.AddYears(1);
            var sut = new MinDateVersionFilter(futureDate);

            sut.Exclude(version, out var reason).ShouldBeTrue();
            reason.ShouldNotBeNullOrWhiteSpace();
        }

        [Test]
        public void WhenShaMismatchShouldNotExclude()
        {
            var commit = new MockCommit(); //when = UtcNow
            var context = new GitVersionContextBuilder().Build();
            var version = new BaseVersion(context, "dummy", false, new SemanticVersion(1), commit, string.Empty);
            var pastDate = DateTimeOffset.UtcNow.AddYears(-1);
            var sut = new MinDateVersionFilter(pastDate);

            sut.Exclude(version, out var reason).ShouldBeFalse();
            reason.ShouldBeNull();
        }

        [Test]
        public void ExcludeShouldAcceptVersionWithNullCommit()
        {
            var context = new GitVersionContextBuilder().Build();
            var version = new BaseVersion(context, "dummy", false, new SemanticVersion(1), null, string.Empty);
            var futureDate = DateTimeOffset.UtcNow.AddYears(1);
            var sut = new MinDateVersionFilter(futureDate);

            sut.Exclude(version, out var reason).ShouldBeFalse();
            reason.ShouldBeNull();
        }
    }
}
