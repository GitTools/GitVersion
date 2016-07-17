using GitVersion;
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
            Should.Throw<ArgumentNullException>(() => sut.Exclude(null, out reason));
        }

        [Test]
        public void WhenCommitShouldExcludeWithReason()
        {
            var commit = new MockCommit(); //when = UtcNow
            var version = new BaseVersion("dummy", false, new SemanticVersion(1), commit, string.Empty);
            var futureDate = DateTimeOffset.UtcNow.AddYears(1);
            var sut = new MinDateVersionFilter(futureDate);

            string reason;
            sut.Exclude(version, out reason).ShouldBeTrue();
            reason.ShouldNotBeNullOrWhiteSpace();
        }

        [Test]
        public void WhenShaMismatchShouldNotExclude()
        {
            var commit = new MockCommit(); //when = UtcNow
            var version = new BaseVersion("dummy", false, new SemanticVersion(1), commit, string.Empty);
            var pastDate = DateTimeOffset.UtcNow.AddYears(-1);
            var sut = new MinDateVersionFilter(pastDate);

            string reason;
            sut.Exclude(version, out reason).ShouldBeFalse();
            reason.ShouldBeNull();
        }

        [Test]
        public void ExcludeShouldAcceptVersionWithNullCommit()
        {
            var version = new BaseVersion("dummy", false, new SemanticVersion(1), null, string.Empty);
            var futureDate = DateTimeOffset.UtcNow.AddYears(1);
            var sut = new MinDateVersionFilter(futureDate);

            string reason;
            sut.Exclude(version, out reason).ShouldBeFalse();
            reason.ShouldBeNull();
        }
    }
}
