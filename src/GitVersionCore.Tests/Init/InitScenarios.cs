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
    public class InitScenarios
    {
        [Test]
        public void CanSetNextVersion()
        {
            var testFileSystem = new TestFileSystem();
            var testConsole = new TestConsole("3", "2.0.0", "0");
            ConfigurationProvider.Init("c:\\proj", testFileSystem, testConsole);

            testFileSystem.ReadAllText("c:\\proj\\GitVersionConfig.yaml").ShouldMatchApproved();
        }

        [Test]
        public void DefaultResponsesDoNotThrow()
        {
            var steps = Types.InAssemblyOf<EditConfigStep>(t => t.IsSubclassOf(typeof(ConfigInitWizardStep)) && t.IsConcreteClass());
            Convention.Is(new InitStepsDefaultResponsesDoNotThrow(), steps);
        }
    }
}