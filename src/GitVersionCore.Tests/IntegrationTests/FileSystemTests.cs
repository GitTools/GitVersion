using System.IO;
using System.Text;

using GitVersion.Helpers;

using NUnit.Framework;

using Shouldly;

[TestFixture]
public class FileSystemTests
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
    public void WhenFileExistsWithEncodingPreamble_EncodingIsPreservedAfterWriteAll(string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);

        File.WriteAllText(TempFilePath, "(－‸ლ)", encoding);

        var fileSystem = new FileSystem();
        fileSystem.WriteAllText(TempFilePath, @"¯\(◉◡◔)/¯");

        using (var stream = File.OpenRead(TempFilePath))
        {
            var preamble = encoding.GetPreamble();
            var bytes = new byte[preamble.Length];
            stream.Read(bytes, 0, preamble.Length);

            bytes.ShouldBe(preamble);
        }
    }

    [Test]
    public void WhenFileDoesNotExist_CreateWithUTF8WithPreamble()
    {
        var encoding = Encoding.UTF8;

        var fileSystem = new FileSystem();
        fileSystem.WriteAllText(TempFilePath, "╚(ಠ_ಠ)=┐");

        using (var stream = File.OpenRead(TempFilePath))
        {
            var preamble = encoding.GetPreamble();
            var bytes = new byte[preamble.Length];
            stream.Read(bytes, 0, preamble.Length);

            bytes.ShouldBe(preamble);
        }
    }
}