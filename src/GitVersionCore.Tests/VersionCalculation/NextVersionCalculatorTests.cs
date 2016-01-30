namespace GitVersionCore.Tests.VersionCalculation
{
    using System;
    using GitVersion;
    using GitVersion.VersionCalculation;
    using NUnit.Framework;
    using Shouldly;

    public class NextVersionCalculatorTests
    {
        [Test]
        public void ShouldIncrementVersionBasedOnConfig()
        {
            var baseCalculator = new TestBaseVersionCalculator(true, new SemanticVersion(1), new MockCommit());
            var semanticVersionBuildMetaData = new SemanticVersionBuildMetaData(1, "master", "b1a34e", DateTimeOffset.Now);
            var sut = new NextVersionCalculator(baseCalculator, new TestMetaDataCalculator(semanticVersionBuildMetaData));
            var config = new Config();
            var context = new GitVersionContextBuilder().WithConfig(config).Build();

            var version = sut.FindVersion(context);

            version.ToString().ShouldBe("1.0.1");
        }

        [Test]
        public void DoesNotIncrementWhenBaseVersionSaysNotTo()
        {
            var baseCalculator = new TestBaseVersionCalculator(false, new SemanticVersion(1), new MockCommit());
            var semanticVersionBuildMetaData = new SemanticVersionBuildMetaData(1, "master", "b1a34e", DateTimeOffset.Now);
            var sut = new NextVersionCalculator(baseCalculator, new TestMetaDataCalculator(semanticVersionBuildMetaData));
            var config = new Config();
            var context = new GitVersionContextBuilder().WithConfig(config).Build();

            var version = sut.FindVersion(context);

            version.ToString().ShouldBe("1.0.0");
        }

        [Test]
        public void AppliesBranchPreReleaseTag()
        {
            var baseCalculator = new TestBaseVersionCalculator(false, new SemanticVersion(1), new MockCommit());
            var semanticVersionBuildMetaData = new SemanticVersionBuildMetaData(2, "develop", "b1a34e", DateTimeOffset.Now);
            var sut = new NextVersionCalculator(baseCalculator, new TestMetaDataCalculator(semanticVersionBuildMetaData));
            var context = new GitVersionContextBuilder()
                .WithDevelopBranch()
                .Build();

            var version = sut.FindVersion(context);

            version.ToString("f").ShouldBe("1.0.0-unstable.1+2");
        }

        [Test]
        public void PreReleaseTagCanUseBranchName()
        {
            var baseCalculator = new TestBaseVersionCalculator(false, new SemanticVersion(1), new MockCommit());
            var semanticVersionBuildMetaData = new SemanticVersionBuildMetaData(2, "develop", "b1a34e", DateTimeOffset.Now);
            var sut = new NextVersionCalculator(baseCalculator, new TestMetaDataCalculator(semanticVersionBuildMetaData));
            var config = new Config();
            config.Branches.Add("custom/", new BranchConfig
            {
                Tag = "useBranchName"
            });
            var context = new GitVersionContextBuilder()
                .WithConfig(config)
                .WithDevelopBranch()
                .AddBranch("custom/foo")
                .Build();

            var version = sut.FindVersion(context);

            version.ToString("f").ShouldBe("1.0.0-foo.1+2");
        }

        [Test]
        public void PreReleaseTagCanUseBranchNameVariable()
        {
            var baseCalculator = new TestBaseVersionCalculator(false, new SemanticVersion(1), new MockCommit());
            var semanticVersionBuildMetaData = new SemanticVersionBuildMetaData(2, "develop", "b1a34e", DateTimeOffset.Now);
            var sut = new NextVersionCalculator(baseCalculator, new TestMetaDataCalculator(semanticVersionBuildMetaData));
            var config = new Config();
            config.Branches.Add("custom/", new BranchConfig
            {
                Tag = "alpha.{BranchName}"
            });
            var context = new GitVersionContextBuilder()
                .WithConfig(config)
                .WithDevelopBranch()
                .AddBranch("custom/foo")
                .Build();

            var version = sut.FindVersion(context);

            version.ToString("f").ShouldBe("1.0.0-alpha.foo.1+2");
        }
    }
}