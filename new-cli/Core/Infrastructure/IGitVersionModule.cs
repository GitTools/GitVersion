using Microsoft.Extensions.DependencyInjection;

namespace Core
{
    public interface IGitVersionModule
    {
        void RegisterTypes(IServiceCollection services);
    }
}