using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ApprovalTests;
using GitVersion;
using GitVersion.Helpers;
using NUnit.Framework;
using Shouldly;
using YamlDotNet.Serialization;

[TestFixture]
public class ConfigProviderTests
{
    string gitDirectory;
    IFileSystem fileSystem;

    [SetUp]
    public void Setup()
    {
        fileSystem = new TestFileSystem();
        gitDirectory = "c:\\MyGitRepo\\.git";
    }

    [Test]
    public void CanReadDocument()
    {
        const string text = @"
assembly-versioning-scheme: MajorMinor
next-version: 2.0.0
tag-prefix: '[vV|version-]'
mode: ContinuousDelivery
branches:
    develop:
        mode: ContinuousDeployment
        tag: dev
    release[/-]:
       mode: continuousDeployment
       tag: rc 
";
        SetupConfigFileContent(text);

        var config = ConfigurationProvider.Provide(gitDirectory, fileSystem);
        config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinor);
        config.NextVersion.ShouldBe("2.0.0");
        config.TagPrefix.ShouldBe("[vV|version-]");
        config.VersioningMode.ShouldBe(VersioningMode.ContinuousDelivery);
        config.Branches["develop"].Tag.ShouldBe("dev");
        config.Branches["release[/-]"].Tag.ShouldBe("rc");
        config.Branches["release[/-]"].VersioningMode.ShouldBe(VersioningMode.ContinuousDeployment);
        config.Branches["develop"].VersioningMode.ShouldBe(VersioningMode.ContinuousDeployment);
    }

    [Test]
    public void CanReadOldDocument()
    {
        const string text = @"
assemblyVersioningScheme: MajorMinor
develop-branch-tag: alpha
release-branch-tag: rc
";
        SetupConfigFileContent(text);
        var error = Should.Throw<OldConfigurationException>(() => ConfigurationProvider.Provide(gitDirectory, fileSystem));
        error.Message.ShouldContainWithoutWhitespace(@"GitVersionConfig.yaml contains old configuration, please fix the following errors:
assemblyVersioningScheme has been replaced by assembly-versioning-scheme
develop-branch-tag has been replaced by branch specific configuration.See https://github.com/ParticularLabs/GitVersion/wiki/Branch-Specific-Configuration
release-branch-tag has been replaced by branch specific configuration.See https://github.com/ParticularLabs/GitVersion/wiki/Branch-Specific-Configuration");
    }

    [Test]
    public void OverwritesDefaultsWithProvidedConfig()
    {
        const string text = @"
next-version: 2.0.0
branches:
    develop:
        mode: ContinuousDeployment
        tag: dev";
        SetupConfigFileContent(text);
        var defaultConfig = new Config();
        var config = ConfigurationProvider.Provide(gitDirectory, fileSystem);

        config.NextVersion.ShouldBe("2.0.0");
        config.AssemblyVersioningScheme.ShouldBe(defaultConfig.AssemblyVersioningScheme);
        config.Branches["develop"].Increment.ShouldBe(defaultConfig.Branches["develop"].Increment);
        config.Branches["develop"].VersioningMode.ShouldBe(defaultConfig.Branches["develop"].VersioningMode);
        config.Branches["develop"].Tag.ShouldBe("dev");
    }

    [Test]
    public void CanProvideConfigForNewBranch()
    {
        const string text = @"
next-version: 2.0.0
branches:
    bug[/-]:
        tag: bugfix";
        SetupConfigFileContent(text);
        var config = ConfigurationProvider.Provide(gitDirectory, fileSystem);
        
        config.Branches["bug[/-]"].Tag.ShouldBe("bugfix");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void CanWriteOutEffectiveConfiguration()
    {
        var config = ConfigurationProvider.GetEffectiveConfigAsString(gitDirectory, fileSystem);

        Approvals.Verify(config);
    }

    [Test]
    public void CanReadDefaultDocument()
    {
        const string text = "";
        SetupConfigFileContent(text);
        var config = ConfigurationProvider.Provide(gitDirectory, fileSystem);
        config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
        config.Branches["develop"].Tag.ShouldBe("unstable");
        config.Branches["release[/-]"].Tag.ShouldBe("beta");
        config.TagPrefix.ShouldBe("[vV]");
        config.NextVersion.ShouldBe(null);
    }

    [Test]
    public void VerifyInit()
    {
        var config = typeof(Config);
        var aliases = config.GetProperties()
            .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() == null)
            .Select(p => ((YamlMemberAttribute) p.GetCustomAttribute(typeof(YamlMemberAttribute))).Alias);
        var writer = new StringWriter();

        ConfigSerialiser.WriteSample(writer);
        var initFile = writer.GetStringBuilder().ToString();

        foreach (var alias in aliases)
        {
            initFile.ShouldContain(alias);
        }
    }

    [Test]
    public void VerifyAliases()
    {
        var config = typeof(Config);
        var propertiesMissingAlias = config.GetProperties()
            .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() == null)
            .Where(p => p.GetCustomAttribute(typeof(YamlMemberAttribute)) == null)
            .Select(p => p.Name);

        propertiesMissingAlias.ShouldBeEmpty();
    }

    void SetupConfigFileContent(string text)
    {
        fileSystem.WriteAllText(Path.Combine(Directory.GetParent(gitDirectory).FullName, "GitVersionConfig.yaml"), text);
    }
}