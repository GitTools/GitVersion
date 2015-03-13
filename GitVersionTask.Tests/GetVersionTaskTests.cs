using System.Linq;
using GitVersion;
using GitVersionTask;
using Microsoft.Build.Framework;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class GetVersionTaskTests
{
    [Test]
    public void OutputsShouldMatchVariableProvider()
    {
        var taskProperties = typeof(GetVersion)
            .GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(OutputAttribute), false).Any())
            .Select(p => p.Name);

        var variablesProperties = typeof(VersionVariables)
            .GetProperties()
            .Select(p => p.Name)
            .Except(new[] { "AvailableVariables", "Item" });

        taskProperties.ShouldBe(variablesProperties, ignoreOrder: true);
    }
}
