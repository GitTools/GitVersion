using GitVersion;
using GitVersion.Helpers;
using NUnit.Framework;
using Shouldly;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;

[TestFixture]
public class ConfigProviderTests
{
    private const string DefaultRepoPath = "c:\\MyGitRepo";
    private const string DefaultWorkingPath = "c:\\MyGitRepo\\Working";

    string repoPath;
    string workingPath;
    IFileSystem fileSystem;

    [SetUp]
    public void Setup()
    {
        fileSystem = new TestFileSystem();
        repoPath = DefaultRepoPath;
        workingPath = DefaultWorkingPath;
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
        config.AssemblyInformationalFormat.ShouldBe(null);
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
        error.Message.ShouldContainWithoutWhitespace(@"GitVersion configuration file contains old configuration, please fix the following errors:
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
    public void CanRemoveTag()
    {
        const string text = @"
next-version: 2.0.0
branches:
    releases?[/-]:
        tag: """"";
        SetupConfigFileContent(text);
        var config = ConfigurationProvider.Provide(repoPath, fileSystem);

        config.NextVersion.ShouldBe("2.0.0");
        config.Branches["releases?[/-]"].Tag.ShouldBe(string.Empty);
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
    public void NextVersionCanBeInteger()
    {
        const string text = "next-version: 2";
        SetupConfigFileContent(text);
        var config = ConfigurationProvider.Provide(repoPath, fileSystem);

        config.NextVersion.ShouldBe("2.0");
    }

    [Test]
    public void NextVersionCanHaveEnormousMinorVersion()
    {
        const string text = "next-version: 2.118998723";
        SetupConfigFileContent(text);
        var config = ConfigurationProvider.Provide(repoPath, fileSystem);

        config.NextVersion.ShouldBe("2.118998723");
    }

    [Test]
    public void NextVersionCanHavePatch()
    {
        const string text = "next-version: 2.12.654651698";
        SetupConfigFileContent(text);
        var config = ConfigurationProvider.Provide(repoPath, fileSystem);

        config.NextVersion.ShouldBe("2.12.654651698");
    }

    [Test]
    [NUnit.Framework.Category("NoMono")]
    [NUnit.Framework.Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void CanWriteOutEffectiveConfiguration()
    {
        var config = ConfigurationProvider.GetEffectiveConfigAsString(repoPath, fileSystem);

        config.ShouldMatchApproved();
    }

    [Test]
    public void CanUpdateAssemblyInformationalVersioningScheme()
    {
        const string text = @"
assembly-versioning-scheme: MajorMinor
assembly-informational-format: '{NugetVersion}'";

        SetupConfigFileContent(text);

        var config = ConfigurationProvider.Provide(repoPath, fileSystem);
        config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinor);
        config.AssemblyInformationalFormat.ShouldBe("{NugetVersion}");
    }

    [Test]
    public void CanUpdateAssemblyInformationalVersioningSchemeWithMultipleVariables()
    {
        const string text = @"
assembly-versioning-scheme: MajorMinor
assembly-informational-format: '{Major}.{Minor}.{Patch}'";

        SetupConfigFileContent(text);

        var config = ConfigurationProvider.Provide(repoPath, fileSystem);
        config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinor);
        config.AssemblyInformationalFormat.ShouldBe("{Major}.{Minor}.{Patch}");
    }


    [Test]
    public void CanUpdateAssemblyInformationalVersioningSchemeWithFullSemVer()
    {
        const string text = @"assembly-versioning-scheme: MajorMinorPatch
assembly-informational-format: '{FullSemVer}'
mode: ContinuousDelivery
next-version: 5.3.0
branches: {}";

        SetupConfigFileContent(text);

        var config = ConfigurationProvider.Provide(repoPath, fileSystem);
        config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
        config.AssemblyInformationalFormat.ShouldBe("{FullSemVer}");
    }

    [Test]
    public void CanReadDefaultDocument()
    {
        const string text = "";
        SetupConfigFileContent(text);
        var config = ConfigurationProvider.Provide(repoPath, fileSystem);
        config.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
        config.AssemblyInformationalFormat.ShouldBe(null);
        config.Branches["dev(elop)?(ment)?$"].Tag.ShouldBe("alpha");
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

    [TestCase(DefaultRepoPath)]
    [TestCase(DefaultWorkingPath)]
    public void WarnOnExistingGitVersionConfigYamlFile(string path)
    {
        SetupConfigFileContent(string.Empty, ConfigurationProvider.ObsoleteConfigFileName, path);

        var logOutput = string.Empty;
        Action<string> action = info => { logOutput = info; };
        using (Logger.AddLoggersTemporarily(action, action, action))
        {
            ConfigurationProvider.Verify(workingPath, repoPath, fileSystem);
        }
        var configFileDeprecatedWarning = string.Format("{0}' is deprecated, use '{1}' instead", ConfigurationProvider.ObsoleteConfigFileName, ConfigurationProvider.DefaultConfigFileName);
        logOutput.Contains(configFileDeprecatedWarning).ShouldBe(true);
    }

    [TestCase(DefaultRepoPath)]
    [TestCase(DefaultWorkingPath)]
    public void WarnOnAmbiguousConfigFilesAtTheSameProjectRootDirectory(string path)
    {
        SetupConfigFileContent(string.Empty, ConfigurationProvider.ObsoleteConfigFileName, path);
        SetupConfigFileContent(string.Empty, ConfigurationProvider.DefaultConfigFileName, path);

        var logOutput = string.Empty;
        Action<string> action = info => { logOutput = info; };
        Logger.SetLoggers(action, action, action);

        ConfigurationProvider.Verify(workingPath, repoPath, fileSystem);

        var configFileDeprecatedWarning = string.Format("Ambiguous config files at '{0}'", path);
        logOutput.Contains(configFileDeprecatedWarning).ShouldBe(true);
    }

    [TestCase(ConfigurationProvider.DefaultConfigFileName, ConfigurationProvider.DefaultConfigFileName)]
    [TestCase(ConfigurationProvider.DefaultConfigFileName, ConfigurationProvider.ObsoleteConfigFileName)]
    [TestCase(ConfigurationProvider.ObsoleteConfigFileName, ConfigurationProvider.DefaultConfigFileName)]
    [TestCase(ConfigurationProvider.ObsoleteConfigFileName, ConfigurationProvider.ObsoleteConfigFileName)]
    public void ThrowsExceptionOnAmbiguousConfigFileLocation(string repoConfigFile, string workingConfigFile)
    {
        var repositoryConfigFilePath = SetupConfigFileContent(string.Empty, repoConfigFile, repoPath);
        var workingDirectoryConfigFilePath = SetupConfigFileContent(string.Empty, workingConfigFile, workingPath);

        WarningException exception = Should.Throw<WarningException>(() => { ConfigurationProvider.Verify(workingPath, repoPath, fileSystem); });

        var expecedMessage = string.Format("Ambiguous config file selection from '{0}' and '{1}'", workingDirectoryConfigFilePath, repositoryConfigFilePath);
        exception.Message.ShouldBe(expecedMessage);
    }

    [Test]
    public void NoWarnOnGitVersionYmlFile()
    {
        SetupConfigFileContent(string.Empty);

        var s = string.Empty;
        Action<string> action = info => { s = info; };
        using (Logger.AddLoggersTemporarily(action, action, action))
        {
            ConfigurationProvider.Provide(repoPath, fileSystem);
        }
        s.Length.ShouldBe(0);
    }

    string SetupConfigFileContent(string text, string fileName = ConfigurationProvider.DefaultConfigFileName)
    {
        return SetupConfigFileContent(text, fileName, repoPath);
    }

    string SetupConfigFileContent(string text, string fileName, string path)
    {
        var fullPath = Path.Combine(path, fileName);
        fileSystem.WriteAllText(fullPath, text);

        return fullPath;
    }
}
