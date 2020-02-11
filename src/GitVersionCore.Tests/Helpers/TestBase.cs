using System;
using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersionCore.Tests.Helpers
{
    public class TestBase
    {
        protected static IServiceProvider ConfigureServices(Action<IServiceCollection> overrideServices = null)
        {
            var services = new ServiceCollection()
                .AddModule(new GitVersionCoreTestModule());

            overrideServices?.Invoke(services);

            return services.BuildServiceProvider();
        }
    }
}
