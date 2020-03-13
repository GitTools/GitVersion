using System.IO;
using System.Text;
using GitVersion;
using GitVersionCore.Tests.Helpers;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class FileSystemTests : TestBase
    {
        public string TempFilePath { get; set; }

        [SetUp]
        public void CreateTempFile()
        {
            TempFilePath = Path.GetTempFileName();
        }

        [TearDown]
        public void Cleanup()
        {
            File.Delete(TempFilePath);
        }

        [TestCase("utf-32")]
        [TestCase("utf-32BE")]
        [TestCase("utf-16")]
        [TestCase("utf-16BE")]
        [TestCase("utf-8")]
        public void WhenFileExistsWithEncodingPreambleEncodingIsPreservedAfterWriteAll(string encodingName)
        {
            var encoding = Encoding.GetEncoding(encodingName);

            File.WriteAllText(TempFilePath, "(－‸ლ)", encoding);

            var fileSystem = new FileSystem();
            fileSystem.WriteAllText(TempFilePath, @"¯\(◉◡◔)/¯");

            using var stream = File.OpenRead(TempFilePath);
            var preamble = encoding.GetPreamble();
            var bytes = new byte[preamble.Length];
            stream.Read(bytes, 0, preamble.Length);

            bytes.ShouldBe(preamble);
        }

        [Test]
        public void WhenFileDoesNotExistCreateWithUtf8WithPreamble()
        {
            var encoding = Encoding.UTF8;

            var fileSystem = new FileSystem();
            fileSystem.WriteAllText(TempFilePath, "╚(ಠ_ಠ)=┐");

            using var stream = File.OpenRead(TempFilePath);
            var preamble = encoding.GetPreamble();
            var bytes = new byte[preamble.Length];
            stream.Read(bytes, 0, preamble.Length);

            bytes.ShouldBe(preamble);
        }
    }
}
