using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Common
{
    public interface IModule
    {
        void RegisterTypes(IServiceCollection services);
    }
}
