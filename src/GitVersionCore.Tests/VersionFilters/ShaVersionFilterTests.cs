using System;
using GitVersion;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using GitVersion.VersionFilters;
using NUnit.Framework;
using Shouldly;

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
            Should.Throw<ArgumentNullException>(() => sut.Exclude(null, out reason));
        }

        [Test]
        public void WhenShaMatchShouldExcludeWithReason()
        {
            var commit = new MockCommit();
            var version = new BaseVersion("dummy", false, new SemanticVersion(1), commit, string.Empty);
            var sut = new ShaVersionFilter(new[] { commit.Sha });

            string reason;
            sut.Exclude(version, out reason).ShouldBeTrue();
            reason.ShouldNotBeNullOrWhiteSpace();
        }

        [Test]
        public void WhenShaMismatchShouldNotExclude()
        {
            var commit = new MockCommit();
            var version = new BaseVersion("dummy", false, new SemanticVersion(1), commit, string.Empty);
            var sut = new ShaVersionFilter(new[] { "mismatched" });

            string reason;
            sut.Exclude(version, out reason).ShouldBeFalse();
            reason.ShouldBeNull();
        }

        [Test]
        public void ExcludeShouldAcceptVersionWithNullCommit()
        {
            var version = new BaseVersion("dummy", false, new SemanticVersion(1), null, string.Empty);
            var sut = new ShaVersionFilter(new[] { "mismatched" });

            string reason;
            sut.Exclude(version, out reason).ShouldBeFalse();
            reason.ShouldBeNull();
        }
    }
}
