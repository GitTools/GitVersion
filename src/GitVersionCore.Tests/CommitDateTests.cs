using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GitVersion;
using GitVersion.VersionFilters;
using NUnit.Framework;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class CommitDateTests
    {
        [Test]
        [TestCase("yyyy-MM-dd", "2017-10-06")]
        [TestCase("dd.MM.yyyy", "06.10.2017")]
        [TestCase("yyyyMMdd", "20171006")]
        [TestCase("yyyy-MM", "2017-10")]
        public void CommitDateFormatTest(string format, string expectedOutcome)
        {
            var date = new DateTime(2017, 10, 6);

            var formatValues = new SemanticVersionFormatValues(
                                    new SemanticVersion
                                    {
                                        BuildMetaData = new SemanticVersionBuildMetaData(0, "master", "3139d4eeb044f46057693473eacc2655b3b27e7d", new DateTimeOffset(date, TimeSpan.Zero)), // assume time zone is UTC

                                    },
                                    new EffectiveConfiguration(
                                        AssemblyVersioningScheme.MajorMinorPatch, AssemblyFileVersioningScheme.MajorMinorPatch, "", "", VersioningMode.ContinuousDelivery, "", "", "", IncrementStrategy.Inherit,
                                        "", true, "", "", false, "", "", "", "", CommitMessageIncrementMode.Enabled, 4, 4, 4, Enumerable.Empty<IVersionFilter>(), false, true, format)
                                );

            Assert.That(formatValues.CommitDate, Is.EqualTo(expectedOutcome));
        }
    }
}
