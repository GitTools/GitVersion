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
        public static IEnumerable<TestCaseData> CommitDateFormatTestCases
        {
            get
            {
                var date = new DateTime(2017, 10, 6);

                var testCasesFormats = new[]
                {
                    "yyyy-MM-dd",
                    "dd.MM.yyyy",
                    "yyyyMMdd",
                    "yyyy-MM"
                };

                foreach (var format in testCasesFormats)
                    yield return new TestCaseData(date, format, date.ToString(format));
            }
        }

        [Test]
        [TestCaseSource(nameof(CommitDateFormatTestCases))]
        public void CommitDateFormatTest(DateTime date, string format, string expectedOutcome)
        {
            var formatValues = new SemanticVersionFormatValues(
                                    new SemanticVersion
                                    {
                                        BuildMetaData = new SemanticVersionBuildMetaData(0, "master", "3139d4eeb044f46057693473eacc2655b3b27e7d", new DateTimeOffset(date, TimeSpan.Zero)), // assume time zone is UTC

                                    },
                                    new EffectiveConfiguration(
                                        AssemblyVersioningScheme.MajorMinorPatch, AssemblyFileVersioningScheme.MajorMinorPatch, "", VersioningMode.ContinuousDelivery, "", "", "", IncrementStrategy.Inherit,
                                        "", true, "", "", false, "", "", "", "", CommitMessageIncrementMode.Enabled, 4, 4, 4, Enumerable.Empty<IVersionFilter>(), false, true, format)
                                );

            Assert.That(formatValues.CommitDate, Is.EqualTo(expectedOutcome));
        }
    }
}
