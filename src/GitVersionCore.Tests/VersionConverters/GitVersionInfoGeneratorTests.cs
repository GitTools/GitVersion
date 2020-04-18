using System;
using System.IO;
using GitVersion;
using GitVersion.VersionCalculation;
using GitVersion.VersionConverters.GitVersionInfo;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public class GitVersionInfoGeneratorTests : TestBase
    {
        [SetUp]
        public void Setup()
        {
            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestCaseAttribute>();
        }

        [TestCase("cs")]
        [TestCase("fs")]
        [TestCase("vb")]
        [Category("NoMono")]
        [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
        public void ShouldCreateFile(string fileExtension)
        {
            var directory = Path.GetTempPath();
            var fileName = "GitVersionInformation.g." + fileExtension;
            var fullPath = Path.Combine(directory, fileName);

            var semanticVersion = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = "unstable4",
                BuildMetaData = new SemanticVersionBuildMetaData("versionSourceSha", 5,
                    "feature1", "commitSha", "commitShortSha", DateTimeOffset.Parse("2014-03-06 23:59:59Z"))
            };

            var sp = ConfigureServices();

            var fileSystem = sp.GetService<IFileSystem>();
            var variableProvider = sp.GetService<IVariableProvider>();

            var variables = variableProvider.GetVariablesFor(semanticVersion, new TestEffectiveConfiguration(), false);
            using var generator = sp.GetService<IGitVersionInfoGenerator>();

            generator.Execute(variables, new GitVersionInfoContext(directory, fileName, fileExtension));

            fileSystem.ReadAllText(fullPath).ShouldMatchApproved(c => c.SubFolder(Path.Combine("Approved", fileExtension)));
        }
    }
}
