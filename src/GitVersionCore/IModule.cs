using Microsoft.Extensions.DependencyInjection;

namespace GitVersion
{
    public interface IModule
    {
        void RegisterTypes(IServiceCollection services);
    }
}
