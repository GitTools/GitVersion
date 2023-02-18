using System.Runtime.CompilerServices;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;

namespace GitVersion.Core.Tests;

[TestFixture]
public class ConfigurationProviderTests : TestBase
{
    private const string DefaultRepoPath = @"c:\MyGitRepo";

    private string repoPath;
    private IConfigurationProvider configurationProvider;
    private IFileSystem fileSystem;

    [SetUp]
    public void Setup()
    {
        this.repoPath = DefaultRepoPath;
        var options = Options.Create(new GitVersionOptions { WorkingDirectory = repoPath });
        var sp = ConfigureServices(services => services.AddSingleton(options));
        this.configurationProvider = sp.GetRequiredService<IConfigurationProvider>();
        this.fileSystem = sp.GetRequiredService<IFileSystem>();

        ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
    }

    [Test]
    public void OverwritesDefaultsWithProvidedConfig()
    {
        var defaultConfig = this.configurationProvider.Provide(this.repoPath);
        const string text = @"
next-version: 2.0.0
branches:
    develop:
        mode: ContinuousDeployment
        label: dev";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.NextVersion.ShouldBe("2.0.0");
        configuration.Branches.ShouldNotBeNull();
        configuration.Branches["develop"].Increment.ShouldBe(defaultConfig.Branches["develop"].Increment);
        configuration.Branches["develop"].VersioningMode.ShouldBe(defaultConfig.Branches["develop"].VersioningMode);
        configuration.Branches["develop"].Label.ShouldBe("dev");
    }

    [Test]
    public void AllBranchesModeWhenUsingMainline()
    {
        const string text = "mode: Mainline";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);
        var branches = configuration.Branches.Select(x => x.Value);
        branches.All(branch => branch.VersioningMode == VersioningMode.Mainline).ShouldBe(true);
    }

    [Test]
    public void CanRemoveLabel()
    {
        const string text = @"
next-version: 2.0.0
branches:
    release:
        label: """"";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.NextVersion.ShouldBe("2.0.0");
        configuration.Branches["release"].Label.ShouldBe(string.Empty);
    }

    [Test]
    public void RegexIsRequired()
    {
        const string text = @"
next-version: 2.0.0
branches:
    bug:
        label: bugfix";
        SetupConfigFileContent(text);
        var ex = Should.Throw<ConfigurationException>(() => this.configurationProvider.Provide(this.repoPath));
        ex.Message.ShouldBe($"Branch configuration 'bug' is missing required configuration 'regex'{System.Environment.NewLine}" +
                            "See https://gitversion.net/docs/reference/configuration for more info");
    }

    [Test]
    public void SourceBranchIsRequired()
    {
        const string text = @"
next-version: 2.0.0
branches:
    bug:
        regex: 'bug[/-]'
        label: bugfix";
        SetupConfigFileContent(text);
        var ex = Should.Throw<ConfigurationException>(() => this.configurationProvider.Provide(this.repoPath));
        ex.Message.ShouldBe($"Branch configuration 'bug' is missing required configuration 'source-branches'{System.Environment.NewLine}" +
                            "See https://gitversion.net/docs/reference/configuration for more info");
    }

    [Test(Description = "This test proves the configuration validation will fail early with a helpful message when a branch listed in source-branches has no configuration.")]
    public void SourceBranchesValidationShouldFailWhenMatchingBranchConfigurationIsMissing()
    {
        const string text = @"
branches:
    bug:
        regex: 'bug[/-]'
        label: bugfix
        source-branches: [notconfigured]";
        SetupConfigFileContent(text);
        var ex = Should.Throw<ConfigurationException>(() => this.configurationProvider.Provide(this.repoPath));
        ex.Message.ShouldBe($"Branch configuration 'bug' defines these 'source-branches' that are not configured: '[notconfigured]'{System.Environment.NewLine}" +
                            "See https://gitversion.net/docs/reference/configuration for more info");
    }

    [Test(Description = "Well-known branches may not be present in the configuration file. This test confirms the validation check succeeds when the source-branches configuration contain these well-known branches.")]
    [TestCase(GitVersionConfiguration.MainBranchKey)]
    [TestCase(GitVersionConfiguration.DevelopBranchKey)]
    public void SourceBranchesValidationShouldSucceedForWellKnownBranches(string wellKnownBranchKey)
    {
        var text = $@"
branches:
    bug:
        regex: 'bug[/-]'
        label: bugfix
        source-branches: [{wellKnownBranchKey}]";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.Branches["bug"].SourceBranches.ShouldBe(new List<string> { wellKnownBranchKey });
    }

    [Test]
    public void CanProvideConfigForNewBranch()
    {
        const string text = @"
next-version: 2.0.0
branches:
    bug:
        regex: 'bug[/-]'
        label: bugfix
        source-branches: []";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.Branches["bug"].Regex.ShouldBe("bug[/-]");
        configuration.Branches["bug"].Label.ShouldBe("bugfix");
    }

    [Test]
    public void MasterConfigReplacedWithMain()
    {
        const string text = @"
next-version: 2.0.0
branches:
    master:
        regex: '^master$|^main$'
        label: beta";
        SetupConfigFileContent(text);

        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.Branches[MainBranch].Regex.ShouldBe("^master$|^main$");
        configuration.Branches[MainBranch].Label.ShouldBe("beta");
    }

    [Test]
    public void MasterConfigReplacedWithMainInSourceBranches()
    {
        const string text = @"
next-version: 2.0.0
branches:
    breaking:
        regex: breaking[/]
        mode: ContinuousDeployment
        increment: Major
        source-branches: ['master']
        is-release-branch: false";
        SetupConfigFileContent(text);

        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.Branches["breaking"].Regex.ShouldBe("breaking[/]");
        configuration.Branches["breaking"].SourceBranches.ShouldHaveSingleItem();
        configuration.Branches["breaking"].SourceBranches?.ShouldContain(MainBranch);
    }

    [Test]
    public void NextVersionCanBeInteger()
    {
        const string text = "next-version: 2";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.NextVersion.ShouldBe("2.0");
    }

    [Test]
    public void NextVersionCanHaveEnormousMinorVersion()
    {
        const string text = "next-version: 2.118998723";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.NextVersion.ShouldBe("2.118998723");
    }

    [Test]
    public void NextVersionCanHavePatch()
    {
        const string text = "next-version: 2.12.654651698";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.NextVersion.ShouldBe("2.12.654651698");
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void CanWriteOutEffectiveConfiguration()
    {
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.ToString().ShouldMatchApproved();
    }

    [Test]
    public void CanUpdateAssemblyInformationalVersioningScheme()
    {
        const string text = @"
assembly-versioning-scheme: MajorMinor
assembly-file-versioning-scheme: MajorMinorPatch
assembly-informational-format: '{NugetVersion}'";

        SetupConfigFileContent(text);

        var configuration = this.configurationProvider.Provide(this.repoPath);
        configuration.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinor);
        configuration.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
        configuration.AssemblyInformationalFormat.ShouldBe("{NugetVersion}");
    }

    [Test]
    public void CanUpdateAssemblyInformationalVersioningSchemeWithMultipleVariables()
    {
        const string text = @"
assembly-versioning-scheme: MajorMinor
assembly-file-versioning-scheme: MajorMinorPatch
assembly-informational-format: '{Major}.{Minor}.{Patch}'";

        SetupConfigFileContent(text);

        var configuration = this.configurationProvider.Provide(this.repoPath);
        configuration.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinor);
        configuration.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
        configuration.AssemblyInformationalFormat.ShouldBe("{Major}.{Minor}.{Patch}");
    }


    [Test]
    public void CanUpdateAssemblyInformationalVersioningSchemeWithFullSemVer()
    {
        const string text = @"assembly-versioning-scheme: MajorMinorPatch
assembly-file-versioning-scheme: MajorMinorPatch
assembly-informational-format: '{FullSemVer}'
mode: ContinuousDelivery
next-version: 5.3.0
branches: {}";

        SetupConfigFileContent(text);

        var configuration = this.configurationProvider.Provide(this.repoPath);
        configuration.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
        configuration.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
        configuration.AssemblyInformationalFormat.ShouldBe("{FullSemVer}");
    }

    [Test]
    public void CanReadDefaultDocument()
    {
        const string text = "";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);
        configuration.AssemblyVersioningScheme.ShouldBe(AssemblyVersioningScheme.MajorMinorPatch);
        configuration.AssemblyFileVersioningScheme.ShouldBe(AssemblyFileVersioningScheme.MajorMinorPatch);
        configuration.AssemblyInformationalFormat.ShouldBe(null);
        configuration.Branches["develop"].Label.ShouldBe("alpha");
        configuration.Branches["release"].Label.ShouldBe("beta");
        configuration.LabelPrefix.ShouldBe(GitVersionConfiguration.DefaultLabelPrefix);
        configuration.NextVersion.ShouldBe(null);
    }

    [Test]
    public void VerifyAliases()
    {
        var configuration = typeof(GitVersionConfiguration);
        var propertiesMissingAlias = configuration.GetProperties()
            .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() == null)
            .Where(p => p.GetCustomAttribute(typeof(YamlMemberAttribute)) == null)
            .Select(p => p.Name);

        propertiesMissingAlias.ShouldBeEmpty();
    }

    [Test]
    public void NoWarnOnGitVersionYmlFile()
    {
        SetupConfigFileContent(string.Empty);

        var stringLogger = string.Empty;
        void Action(string info) => stringLogger = info;

        var logAppender = new TestLogAppender(Action);
        var log = new Log(logAppender);

        var options = Options.Create(new GitVersionOptions { WorkingDirectory = repoPath });
        var sp = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<ILog>(log);
        });
        this.configurationProvider = sp.GetRequiredService<IConfigurationProvider>();

        this.configurationProvider.Provide(this.repoPath);

        stringLogger.Length.ShouldBe(0);
    }

    private void SetupConfigFileContent(string text, string fileName = ConfigurationFileLocator.DefaultFileName) => SetupConfigFileContent(text, fileName, this.repoPath);

    private void SetupConfigFileContent(string text, string fileName, string path)
    {
        var fullPath = PathHelper.Combine(path, fileName);
        this.fileSystem.WriteAllText(fullPath, text);
    }

    [Test]
    public void ShouldUseSpecifiedSourceBranchesForDevelop()
    {
        const string text = @"
next-version: 2.0.0
branches:
    develop:
        mode: ContinuousDeployment
        source-branches: ['develop']
        label: dev";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.Branches["develop"].SourceBranches.ShouldBe(new List<string> { "develop" });
    }

    [Test]
    public void ShouldUseDefaultSourceBranchesWhenNotSpecifiedForDevelop()
    {
        const string text = @"
next-version: 2.0.0
branches:
    develop:
        mode: ContinuousDeployment
        label: dev";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.Branches["develop"].SourceBranches.ShouldBe(new List<string>());
    }

    [Test]
    public void ShouldUseSpecifiedSourceBranchesForFeature()
    {
        const string text = @"
next-version: 2.0.0
branches:
    feature:
        mode: ContinuousDeployment
        source-branches: ['develop', 'release']
        label: dev";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.Branches["feature"].SourceBranches.ShouldBe(new List<string> { "develop", "release" });
    }

    [Test]
    public void ShouldUseDefaultSourceBranchesWhenNotSpecifiedForFeature()
    {
        const string text = @"
next-version: 2.0.0
branches:
    feature:
        mode: ContinuousDeployment
        label: dev";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.Branches["feature"].SourceBranches.ShouldBe(
            new List<string> { "develop", MainBranch, "release", "feature", "support", "hotfix" });
    }

    [Test]
    public void ShouldNotOverrideAnythingWhenOverrideConfigIsEmpty()
    {
        const string text = @"
next-version: 1.2.3
label-prefix: custom-label-prefix-from-yml";
        SetupConfigFileContent(text);

        var expectedConfig = this.configurationProvider.Provide(this.repoPath);
        var overridenConfig = this.configurationProvider.Provide(this.repoPath, new GitVersionConfiguration());

        overridenConfig.AssemblyVersioningScheme.ShouldBe(expectedConfig.AssemblyVersioningScheme);
        overridenConfig.AssemblyFileVersioningScheme.ShouldBe(expectedConfig.AssemblyFileVersioningScheme);
        overridenConfig.AssemblyInformationalFormat.ShouldBe(expectedConfig.AssemblyInformationalFormat);
        overridenConfig.AssemblyVersioningFormat.ShouldBe(expectedConfig.AssemblyVersioningFormat);
        overridenConfig.AssemblyFileVersioningFormat.ShouldBe(expectedConfig.AssemblyFileVersioningFormat);
        overridenConfig.VersioningMode.ShouldBe(expectedConfig.VersioningMode);
        overridenConfig.LabelPrefix.ShouldBe(expectedConfig.LabelPrefix);
        overridenConfig.ContinuousDeploymentFallbackLabel.ShouldBe(expectedConfig.ContinuousDeploymentFallbackLabel);
        overridenConfig.NextVersion.ShouldBe(expectedConfig.NextVersion);
        overridenConfig.MajorVersionBumpMessage.ShouldBe(expectedConfig.MajorVersionBumpMessage);
        overridenConfig.MinorVersionBumpMessage.ShouldBe(expectedConfig.MinorVersionBumpMessage);
        overridenConfig.PatchVersionBumpMessage.ShouldBe(expectedConfig.PatchVersionBumpMessage);
        overridenConfig.NoBumpMessage.ShouldBe(expectedConfig.NoBumpMessage);
        overridenConfig.LabelPreReleaseWeight.ShouldBe(expectedConfig.LabelPreReleaseWeight);
        overridenConfig.CommitMessageIncrementing.ShouldBe(expectedConfig.CommitMessageIncrementing);
        overridenConfig.Increment.ShouldBe(expectedConfig.Increment);
        overridenConfig.CommitDateFormat.ShouldBe(expectedConfig.CommitDateFormat);
        overridenConfig.MergeMessageFormats.ShouldBe(expectedConfig.MergeMessageFormats);
        overridenConfig.UpdateBuildNumber.ShouldBe(expectedConfig.UpdateBuildNumber);

        overridenConfig.Ignore.ShouldBeEquivalentTo(expectedConfig.Ignore);

        overridenConfig.Branches.Keys.ShouldBe(expectedConfig.Branches.Keys);

        foreach (var branch in overridenConfig.Branches.Keys)
        {
            overridenConfig.Branches[branch].ShouldBeEquivalentTo(expectedConfig.Branches[branch]);
        }
    }

    [Test]
    public void ShouldUseDefaultTagPrefixWhenNotSetInConfigFile()
    {
        const string text = "";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.LabelPrefix.ShouldBe(GitVersionConfiguration.DefaultLabelPrefix);
    }

    [Test]
    public void ShouldUseTagPrefixFromConfigFileWhenProvided()
    {
        const string text = "label-prefix: custom-label-prefix-from-yml";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath);

        configuration.LabelPrefix.ShouldBe("custom-label-prefix-from-yml");
    }

    [Test]
    public void ShouldOverrideTagPrefixWithOverrideConfigValue([Values] bool tagPrefixSetAtYmlFile)
    {
        var text = tagPrefixSetAtYmlFile ? "label-prefix: custom-label-prefix-from-yml" : "";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath, new GitVersionConfiguration { LabelPrefix = "label-prefix-from-override-configuration" });

        configuration.LabelPrefix.ShouldBe("label-prefix-from-override-configuration");
    }

    [Test]
    public void ShouldNotOverrideDefaultTagPrefixWhenNotSetInOverrideConfig()
    {
        const string text = "";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath, new GitVersionConfiguration { LabelPrefix = null });

        configuration.LabelPrefix.ShouldBe(GitVersionConfiguration.DefaultLabelPrefix);
    }

    [Test]
    public void ShouldNotOverrideTagPrefixFromConfigFileWhenNotSetInOverrideConfig()
    {
        const string text = "label-prefix: custom-label-prefix-from-yml";
        SetupConfigFileContent(text);
        var configuration = this.configurationProvider.Provide(this.repoPath, new GitVersionConfiguration { LabelPrefix = null });

        configuration.LabelPrefix.ShouldBe("custom-label-prefix-from-yml");
    }
}
