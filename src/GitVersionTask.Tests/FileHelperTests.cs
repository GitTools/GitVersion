using GitVersion.MSBuildTask;
using NUnit.Framework;

namespace GitVersionTask.Tests
{
    [TestFixture]
    public class FileHelperTests
    {
        [Test]
        [TestCase("C#", "cs")]
        [TestCase("F#", "fs")]
        [TestCase("VB", "vb")]
        [TestCase("XY", null)]
        public void GetFileExtensionShouldReturnCorrectExtension(string language, string expectedExtension)
        {
            if (expectedExtension != null)
            {
                Assert.That(FileHelper.GetFileExtension(language), Is.EqualTo(expectedExtension));
            }
            else
            {
                Assert.That(() => FileHelper.GetFileExtension(language), Throws.ArgumentException.With.Message.EqualTo($"Unknown language detected: '{language}'"));
            }
        }

        [Test]
        public void GetFileWriteInfoShouldCreateConstantNamedFileWhenIntermediateOutputPath()
        {
            var fileInfo = "MyIntermediateOutputPath".GetFileWriteInfo("C#", "MyProject.csproj", "GeneratedVersionInformation");

            Assert.That(fileInfo.WorkingDirectory, Is.EqualTo("MyIntermediateOutputPath"));
            Assert.That(fileInfo.FileName, Is.EqualTo("GeneratedVersionInformation.g.cs"));
            Assert.That(fileInfo.FileExtension, Is.EqualTo("cs"));
        }

        [Test]
        public void GetFileWriteInfoShouldCreateRandomNamedFileWhenNoIntermediateOutputPath()
        {
            var fileInfo = FileHelper.GetFileWriteInfo(null, "C#", "MyProject.csproj", "GeneratedVersionInformation");

            Assert.That(fileInfo.WorkingDirectory, Is.EqualTo(FileHelper.TempPath));
            Assert.That(fileInfo.FileName, Does.StartWith("GeneratedVersionInformation_MyProject_").And.EndsWith(".g.cs"));
            Assert.That(fileInfo.FileExtension, Is.EqualTo("cs"));
        }
    }
}
