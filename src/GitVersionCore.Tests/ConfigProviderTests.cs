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
    string repoPath;
    IFileSystem fileSystem;

    [SetUp]
    public void Setup()
    {
        fileSystem = new TestFileSystem();
        repoPath = "c:\\MyGitRepo";
    }

    [Test]
    public void CanReadDocumentAndMigrate()
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

        var config = ConfigurationProvider.Provide(repoPath, fileSystem);
        config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinor);
        config.NextVersion.ShouldBe("2.0.0");
        config.TagPrefix.ShouldBe("[vV|version-]");
        config.VersioningMode.ShouldBe(VersioningMode.ContinuousDelivery);
        config.Branches["dev(elop)?(ment)?$"].Tag.ShouldBe("dev");
        config.Branches["releases?[/-]"].Tag.ShouldBe("rc");
        config.Branches["releases?[/-]"].VersioningMode.ShouldBe(VersioningMode.ContinuousDeployment);
        config.Branches["dev(elop)?(ment)?$"].VersioningMode.ShouldBe(VersioningMode.ContinuousDeployment);
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
        var error = Should.Throw<OldConfigurationException>(() => ConfigurationProvider.Provide(repoPath, fileSystem));
        error.Message.ShouldContainWithoutWhitespace(@"GitVersionConfig.yaml contains old configuration, please fix the following errors:
assemblyVersioningScheme has been replaced by assembly-versioning-scheme
develop-branch-tag has been replaced by branch specific configuration.See http://gitversion.readthedocs.org/en/latest/configuration/#branch-configuration
release-branch-tag has been replaced by branch specific configuration.See http://gitversion.readthedocs.org/en/latest/configuration/#branch-configuration");
    }

    [Test]
    public void OverwritesDefaultsWithProvidedConfig()
    {
        var defaultConfig = ConfigurationProvider.Provide(repoPath, fileSystem);
        const string text = @"
next-version: 2.0.0
branches:
    dev(elop)?(ment)?$:
        mode: ContinuousDeployment
        tag: dev";
        SetupConfigFileContent(text);
        var config = ConfigurationProvider.Provide(repoPath, fileSystem);

        config.NextVersion.ShouldBe("2.0.0");
        config.Branches["dev(elop)?(ment)?$"].Increment.ShouldBe(defaultConfig.Branches["dev(elop)?(ment)?$"].Increment);
        config.Branches["dev(elop)?(ment)?$"].VersioningMode.ShouldBe(defaultConfig.Branches["dev(elop)?(ment)?$"].VersioningMode);
        config.Branches["dev(elop)?(ment)?$"].Tag.ShouldBe("dev");
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
        var config = ConfigurationProvider.Provide(repoPath, fileSystem);
        
        config.Branches["bug[/-]"].Tag.ShouldBe("bugfix");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void CanWriteOutEffectiveConfiguration()
    {
        var config = ConfigurationProvider.GetEffectiveConfigAsString(repoPath, fileSystem);

        Approvals.Verify(config);
    }

    [Test]
    public void CanReadDefaultDocument()
    {
        const string text = "";
        SetupConfigFileContent(text);
        var config = ConfigurationProvider.Provide(repoPath, fileSystem);
        config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
        config.Branches["dev(elop)?(ment)?$"].Tag.ShouldBe("unstable");
        config.Branches["releases?[/-]"].Tag.ShouldBe("beta");
        config.TagPrefix.ShouldBe(ConfigurationProvider.DefaultTagPrefix);
        config.NextVersion.ShouldBe(null);
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
        fileSystem.WriteAllText(Path.Combine(repoPath, "GitVersionConfig.yaml"), text);
    }
}