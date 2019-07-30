namespace GitVersionCore.Tests.Init
{
    using System;
    using System.Linq;
    using System.Reflection;
    using GitVersion.Configuration.Init.Wizard;
    using TestStack.ConventionTests;
    using TestStack.ConventionTests.ConventionData;

    public class InitStepsDefaultResponsesDoNotThrow : IConvention<Types>
    {
        public void Execute(Types data, IConventionResultContext result)
        {
            var resultProperty = typeof(ConfigInitWizardStep).GetProperty("DefaultResult", BindingFlags.NonPublic | BindingFlags.Instance);
            result
                .Is("Init steps default response should not throw",
                data.TypesToVerify.Where(t =>
                {
                    var constructorInfo = t.GetConstructors().Single();
                    var ctorArguments = constructorInfo.GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();
                    var instance = Activator.CreateInstance(t, ctorArguments);
                    try
                    {
                        if (resultProperty != null) resultProperty.GetValue(instance);
                    }
                    catch (Exception)
                    {
                        return true;
                    }
                    return false;
                }));
        }

        public string ConventionReason => "So things do not blow up";
    }
}
