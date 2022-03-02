using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Configuration.Init.Wizard;

public class ConfigInitStepFactory : IConfigInitStepFactory
{
    private readonly IServiceProvider? sp;

    public ConfigInitStepFactory()
    {
    }

    public ConfigInitStepFactory(IServiceProvider sp) => this.sp = sp.NotNull();

    public T? CreateStep<T>() => this.sp!.GetService<T>();
}
