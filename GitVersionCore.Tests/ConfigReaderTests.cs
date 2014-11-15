using System.IO;
using GitVersion;
using GitVersion.Configuration;
using NUnit.Framework;

[TestFixture]
public class ConfigReaderTests
{

    [Test]
    public void CanReadDocument()
    {
        var text = "assemblyVersioningScheme: MajorMinor";
        var config = ConfigReader.Read(new StringReader(text));
        Assert.AreEqual(AssemblyVersioningScheme.MajorMinor, config.AssemblyVersioningScheme);
    }

    [Test]
    public void CanReadDefaultDocument()
    {
        var text = "";
        var config = ConfigReader.Read(new StringReader(text));
        Assert.AreEqual(AssemblyVersioningScheme.MajorMinorPatch, config.AssemblyVersioningScheme);
    }
}