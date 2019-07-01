namespace GitVersionCore.Tests.Init
{
    using GitVersion;
    using GitVersion.Configuration.Init;
    using GitVersion.Configuration.Init.Wizard;
    using NUnit.Framework;
    using Shouldly;
    using TestStack.ConventionTests;
    using TestStack.ConventionTests.ConventionData;

    [TestFixture]
    public class InitScenarios : TestBase
    {
        [SetUp]
        public void Setup()
        {
            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        [Category("NoMono")]
        [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
        public void CanSetNextVersion()
        {
            var testFileSystem = new TestFileSystem();
            var testConsole = new TestConsole("3", "2.0.0", "0");
            var configFileLocator = new DefaultConfigFileLocator();
            ConfigurationProvider.Init("c:\\proj", testFileSystem, testConsole, configFileLocator);

            testFileSystem.ReadAllText("c:\\proj\\GitVersion.yml").ShouldMatchApproved();
        }

        [Test]
        public void DefaultResponsesDoNotThrow()
        {
            var steps = Types.InAssemblyOf<EditConfigStep>(t => t.IsSubclassOf(typeof(ConfigInitWizardStep)) && t.IsConcreteClass());
            Convention.Is(new InitStepsDefaultResponsesDoNotThrow(), steps);
        }
    }
}