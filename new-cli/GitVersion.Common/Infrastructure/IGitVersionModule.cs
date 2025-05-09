using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Infrastructure;

public interface IGitVersionModule
{
    void RegisterTypes(IServiceCollection services);
}
