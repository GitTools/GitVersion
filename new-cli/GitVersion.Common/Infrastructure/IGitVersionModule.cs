using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Infrastructure;

public interface IGitVersionModule
{
    IServiceCollection RegisterTypes(IServiceCollection services);
}
