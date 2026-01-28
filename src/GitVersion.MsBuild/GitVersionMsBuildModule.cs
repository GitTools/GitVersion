namespace GitVersion.MsBuild;

public class GitVersionMsBuildModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services) => services.AddSingleton<IGitVersionTaskExecutor, GitVersionTaskExecutor>();
}
