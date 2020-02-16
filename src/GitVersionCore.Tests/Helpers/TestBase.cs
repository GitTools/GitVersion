using System;
using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersionCore.Tests.Helpers
{
    public class TestBase
    {
        public const string NoMonoDescription = "Won't run on Mono due to source information not being available for ShouldMatchApproved.";
        public const string NoMono = "NoMono";

        protected static IServiceProvider ConfigureServices(Action<IServiceCollection> overrideServices = null)
        {
            var services = new ServiceCollection()
                .AddModule(new GitVersionCoreTestModule());

            overrideServices?.Invoke(services);

            return services.BuildServiceProvider();
        }
    }
}
