namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    using GitVersion.VersionCalculation.BaseVersionCalculators;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class LastTagBaseVersionStrategyTests
    {
        [Test]
        public void ShouldAllowVersionIncrement()
        {
            var context = new GitVersionContextBuilder()
                .WithTaggedMaster()
                .AddCommit()
                .Build();
            var sut = new LastTagBaseVersionStrategy();

            var baseVersion = sut.GetVersion(context);

            baseVersion.ShouldIncrement.ShouldBe(true);
        }

        [Test]
        public void ShouldNotAllowVersionIncrementWhenTagComesFromCurrentCommit()
        {
            var context = new GitVersionContextBuilder()
                .WithTaggedMaster()
                .Build();
            var sut = new LastTagBaseVersionStrategy();

            var baseVersion = sut.GetVersion(context);

            baseVersion.ShouldIncrement.ShouldBe(false);
        }
    }
}