using Microsoft.Extensions.Hosting;

namespace GitVersion.App.Tests;

[TestFixture]
[NonParallelizable]
public class ProgramFixtureTests
{
    [Test]
    public async Task RunReturnsInvocationExitCodeWhenProcessExitCodeChangesDuringShutdown()
    {
        var previousExitCode = SysEnv.ExitCode;
        try
        {
            var fixture = new ProgramFixture();
            fixture.WithOverrides(services => services.AddSingleton(provider =>
            {
                provider.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() => SysEnv.ExitCode = 1);
                return ActivatorUtilities.CreateInstance<GitVersionApp>(provider);
            }));

            var result = await fixture.Run("--version");

            result.ExitCode.ShouldBe(0);
            SysEnv.ExitCode.ShouldBe(1);
        }
        finally
        {
            SysEnv.ExitCode = previousExitCode;
        }
    }
}
