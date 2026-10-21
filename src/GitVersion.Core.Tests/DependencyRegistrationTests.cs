using GitVersion;
using GitVersion.Agents;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Tests;

[TestFixture]
public class DependencyRegistrationTests
{
    [Test]
    public void VersionStrategiesPreserveTheCompleteDiscoveryOrder()
    {
        var services = new ServiceCollection();
        new VersionStrategyModule().RegisterTypes(services);

        var expected = typeof(VersionStrategyModule).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IVersionStrategy).IsAssignableFrom(type));

        services.Select(service => service.ImplementationType).ShouldBe(expected);
        services.ShouldAllBe(service => service.ServiceType == typeof(IVersionStrategy) && service.Lifetime == ServiceLifetime.Singleton);
    }

    [Test]
    public void BuildAgentsPreserveTheCompleteDiscoveryOrder()
    {
        var services = new ServiceCollection();
        new GitVersionBuildAgentsModule().RegisterTypes(services);

        var expected = typeof(GitVersionBuildAgentsModule).Assembly.GetTypes()
            .Where(type => type != typeof(BuildAgentBase) && typeof(BuildAgentBase).IsAssignableFrom(type));

        services.Select(service => service.ImplementationType).ShouldBe(expected);
        services.ShouldAllBe(service => service.ServiceType == typeof(IBuildAgent) && service.Lifetime == ServiceLifetime.Singleton);
    }
}
