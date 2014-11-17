using System.IO;
using GitVersion;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class ConfigReaderTests
{

    [Test]
    public void CanReadDocument()
    {
        const string text = @"
assemblyVersioningScheme: MajorMinor
develop-branch-tag: alpha
release-branch-tag: rc
tag-prefix: '[vV|version-]'
";
        var config = ConfigReader.Read(new StringReader(text));
        config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinor);
        config.DevelopBranchTag.ShouldBe("alpha");
        config.ReleaseBranchTag.ShouldBe("rc");
        config.TagPrefix.ShouldBe("[vV|version-]");
    }

    [Test]
    public void CanReadDefaultDocument()
    {
        const string text = "";
        var config = ConfigReader.Read(new StringReader(text));
        config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
        config.DevelopBranchTag.ShouldBe("unstable");
        config.ReleaseBranchTag.ShouldBe("beta");
        config.TagPrefix.ShouldBe("v");
    }
}