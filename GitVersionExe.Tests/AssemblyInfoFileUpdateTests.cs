namespace GitVersionExe.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using GitVersion;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class AssemblyInfoFileUpdateTests
    {
        [Test]
        public void ShouldStartSearchFromWorkingDirectory()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            const string workingDir = "C:\\Testing";
            using (new AssemblyInfoFileUpdate(new Arguments{ UpdateAssemblyInfo = true }, workingDir, new Dictionary<string, string>(), fileSystem))
            {
                fileSystem.Received().DirectoryGetFiles(Arg.Is(workingDir), Arg.Any<string>(), Arg.Any<SearchOption>());
            }
        }
    }
}