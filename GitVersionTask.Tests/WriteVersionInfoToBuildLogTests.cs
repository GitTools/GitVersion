
namespace GitVersionTask.Tests
{
    using System.IO;
    using NUnit.Framework;

    [TestFixture]
    public class WriteVersionInfoToBuildLogTests
    {
        [Test]
        public void UsingInvalidGitDirectory_ThrowsDirectoryNotFoundException()
        {
            var task = new WriteVersionInfoToBuildLog
            {
                BuildEngine = new MockBuildEngine(),
                SolutionDirectory = Path.GetTempPath()
            };

            Assert.That(task.InnerExecute, Throws.InstanceOf<DirectoryNotFoundException>());
        }
    }
}
