using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace GitVersionCore.Tests.Helpers
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFakeSingleton<T>(this IServiceCollection serviceCollection) where T : class => serviceCollection.AddSingleton(Substitute.For<T>());
    }
}
