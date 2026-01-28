using GitVersion.Extensions;

namespace GitVersion;

public interface IGitVersionModule
{
    void RegisterTypes(IServiceCollection services);

    static IEnumerable<Type> FindAllDerivedTypes<T>(Assembly? assembly)
    {
        assembly.NotNull();

        var derivedType = typeof(T);
        return assembly.GetTypes().Where(t => t != derivedType && derivedType.IsAssignableFrom(t));
    }
}
