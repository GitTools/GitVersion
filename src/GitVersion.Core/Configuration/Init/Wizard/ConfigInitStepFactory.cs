using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Configuration.Init.Wizard;

internal class ConfigInitStepFactory : IConfigInitStepFactory
{
    private readonly IServiceProvider sp;

    public ConfigInitStepFactory()
    {
    }

    public ConfigInitStepFactory(IServiceProvider sp) => this.sp = sp.NotNull();

    public T CreateStep<T>() where T : notnull => this.sp.GetRequiredService<T>();
}
