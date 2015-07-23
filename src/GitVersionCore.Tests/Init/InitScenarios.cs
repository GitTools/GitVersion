namespace GitVersionCore.Tests.Init
{
    using GitVersion;
    using GitVersion.Configuration.Init;
    using GitVersion.Configuration.Init.Wizard;
    using NUnit.Framework;
    using TestStack.ConventionTests;
    using TestStack.ConventionTests.ConventionData;

    [TestFixture]
    public class InitScenarios
    {
        [Test]
        public void DefaultResponsesDoNotThrow()
        {
            var steps = Types.InAssemblyOf<EditConfigStep>(t => t.IsSubclassOf(typeof(ConfigInitWizardStep)) && t.IsConcreteClass());
            Convention.Is(new InitStepsDefaultResponsesDoNotThrow(), steps);
        }
    }
}