using GitVersion;

try
{
    var builder = CliHost.CreateCliHostBuilder(args);

    using var host = builder.Build();
    var app = host.Services.GetRequiredService<GitVersionApp>();

    await app.RunAsync(CancellationToken.None).ConfigureAwait(false);
}
catch (WarningException exception)
{
    await Console.Error.WriteLineAsync("An error occurred:").ConfigureAwait(false);
    await Console.Error.WriteLineAsync(exception.Message).ConfigureAwait(false);
    SysEnv.ExitCode = 1;
}
