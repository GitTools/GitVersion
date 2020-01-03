using Microsoft.Extensions.DependencyInjection;

namespace GitVersion
{
    public interface IGitVersionModule
    {
        void RegisterTypes(IServiceCollection services);
    }
}
