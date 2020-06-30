using GitVersion;
using GitVersion.Extensions;
using GitVersion.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersionCore.Tests.Helpers
{
    public class GitVersionCoreTestModule : IGitVersionModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddModule(new GitVersionCoreModule());

            services.AddSingleton<IFileSystem, TestFileSystem>();
            services.AddSingleton<IEnvironment, TestEnvironment>();
            services.AddSingleton<ILog, NullLog>();
        }
    }
}
