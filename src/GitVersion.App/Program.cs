using GitVersion;

var builder = CliHost.CreateCliHostBuilder(args);

var host = builder.Build();
var app = host.Services.GetRequiredService<GitVersionApp>();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) =>
{
    cts.Cancel();
    cts.Dispose();
};

await app.RunAsync(cts.Token).ConfigureAwait(false);
