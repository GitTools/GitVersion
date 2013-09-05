using System;
using System.Diagnostics;
using System.IO;
using Mono.Cecil;
using NUnit.Framework;

[TestFixture]
public class AssemblyWithNoInfoVersionTests
{
    string beforeAssemblyPath;
    string afterAssemblyPath;

    public AssemblyWithNoInfoVersionTests()
    {
        beforeAssemblyPath = Path.GetFullPath(@"Assemblies\AssemblyWithNoInfoVer.dll");
        afterAssemblyPath = beforeAssemblyPath.Replace(".dll", "2.dll");
        File.Copy(beforeAssemblyPath, afterAssemblyPath, true);
        WeavingHelper.WeaveAssembly(afterAssemblyPath);
    }

    [Test]
    public void EnsureAttributeExists()
    {
        var moduleDefinition = ModuleDefinition.ReadModule(afterAssemblyPath);
        Assert.AreEqual(new Version(1, 2, 3, 0), moduleDefinition.Assembly.Name.Version);
        var infoVersion = moduleDefinition.InfoVersion();
        Assert.IsTrue(infoVersion.StartsWith("1.2.3 Head:'master' Sha:"));
    }

    [Test]
    public void Win32Resource()
    {
        var versionInfo = FileVersionInfo.GetVersionInfo(afterAssemblyPath);
        Assert.IsNotNullOrEmpty(versionInfo.ProductVersion);
        Assert.IsNotNullOrEmpty(versionInfo.FileVersion);
    }


#if(DEBUG)
    [Test]
    public void PeVerify()
    {
        Verifier.Verify(beforeAssemblyPath, afterAssemblyPath);
    }
#endif
}