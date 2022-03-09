using Microsoft.Extensions.DependencyInjection;

namespace Common.Utilities;

public static class ServicesExtensions
{
    public static IServiceCollection UseDotnetTool(this IServiceCollection services, string toolName, string toolVersion)
    {
        services.UseTool(new Uri($"dotnet:?package={toolName}&version={toolVersion}"));
        return services;
    }

    public static IServiceCollection UseNugetTool(this IServiceCollection services, string toolName, string toolVersion)
    {
        services.UseTool(new Uri($"nuget:?package={toolName}&version={toolVersion}"));
        return services;
    }
}
