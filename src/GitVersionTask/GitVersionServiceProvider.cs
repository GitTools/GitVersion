using GitVersion.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.CompilerServices;

namespace GitVersionTask
{
    public readonly struct GitVersionServiceProvider : IDisposableServiceProvider
    {
        private readonly ServiceProvider serviceProvider;

        public GitVersionServiceProvider(ServiceProvider serviceProvider) =>
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        private void ensureNonServiceProvider([CallerMemberName] string memberName = "")
        {
            if (serviceProvider is null)
            {
                throw new InvalidOperationException($"{memberName} is not available because service provider is null.");
            }
        }

        public object GetService(Type serviceType)
        {
            ensureNonServiceProvider();
            return serviceProvider.GetService(serviceType);
        }

        public void Dispose()
        {
            ensureNonServiceProvider();
            serviceProvider.Dispose();
        }
    }
}
