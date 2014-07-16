using System;
using GitVersion;
using NUnit.Framework;

[TestFixture]
internal class SemanticVersionPreReleaseTagTests
{
    [TestCase("Beta")]
    [TestCase("beta")]
    public void AlwaysProducesLowercaseName(string versionString)
    {
        var tagWithConstructor = new SemanticVersionPreReleaseTag(versionString, 0);
        var tagWithProperty = new SemanticVersionPreReleaseTag(versionString, 0);
        tagWithConstructor.Name = versionString;

        Assert.IsTrue(string.Equals("beta", tagWithConstructor.Name, StringComparison.Ordinal));
        Assert.IsTrue(string.Equals("beta", tagWithProperty.Name, StringComparison.Ordinal));
    }
}