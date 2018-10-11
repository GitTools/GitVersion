using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitVersion;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class GitVersionInformationGeneratorTests : TestBase
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
            var fileSystem = new TestFileSystem();
            var directory = Path.GetTempPath();
            var fileName = "GitVersionInformation.g." + fileExtension;
            var fullPath = Path.Combine(directory, fileName);

            var semanticVersion = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = "unstable4",
                BuildMetaData = new SemanticVersionBuildMetaData(5,
                    "feature1", "commitSha", "commitShortSha", DateTimeOffset.Parse("2014-03-06 23:59:59Z"))
            };

            var variables = VariableProvider.GetVariablesFor(semanticVersion, new TestEffectiveConfiguration(), false);
            var generator = new GitVersionInformationGenerator(fileName, directory, variables, fileSystem);

            generator.Generate();

            fileSystem.ReadAllText(fullPath).ShouldMatchApproved(c => c.SubFolder(Path.Combine("Approved", fileExtension)));
        }
    }
}
