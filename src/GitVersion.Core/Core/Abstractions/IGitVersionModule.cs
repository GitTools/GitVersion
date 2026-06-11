using GitVersion.Extensions;

namespace GitVersion;

/// <summary>Represents a self-contained IoC registration module that adds a cohesive set of services to the DI container.</summary>
public interface IGitVersionModule
{
    /// <summary>Registers the services provided by this module into <paramref name="services"/>.</summary>
    void RegisterTypes(IServiceCollection services);

    /// <summary>Returns all concrete types in <paramref name="assembly"/> that are assignable to <typeparamref name="T"/>.</summary>
    static IEnumerable<Type> FindAllDerivedTypes<T>(Assembly? assembly)
    {
        assembly.NotNull();

        var derivedType = typeof(T);
        return assembly.GetTypes().Where(t => t != derivedType && derivedType.IsAssignableFrom(t));
    }
}
