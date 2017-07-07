using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitVersion;
using NUnit.Framework;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class GitVersionInformationGeneratorTests
    {
        [Test]
        public void ShouldCreateFile()
        {
            var fileSystem = new TestFileSystem();
            var directory = Path.GetTempPath();
            var fileName = "GitVersionInformation.g.cs";
            var fullPath = Path.Combine(directory, fileName);
            var variables = VariableProvider.GetVariablesFor(SemanticVersion.Parse("1.0.0", "v"), new TestEffectiveConfiguration(), false);

            var generator = new GitVersionInformationGenerator(fileName, directory, variables, fileSystem);

            generator.Generate();

            var fileContents = fileSystem.ReadAllText(fullPath);
        }
    }
}
