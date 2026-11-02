using System.Net.Http;

namespace GitVersion.Telemetry;

/// <summary>Sends one telemetry request without retries or response-body processing.</summary>
internal interface ITelemetryTransport
{
    /// <summary>Honors cancellation and releases the client and response before completing a single request.</summary>
    Task SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}

/// <summary>Owns short-lived, bounded HTTP clients for policy-approved telemetry requests.</summary>
internal class TelemetryTransport : ITelemetryTransport
{
    /// <summary>Caps connection establishment and client lifetime independently of the caller's shorter deadline.</summary>
    internal static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Sends headers only and disposes transport resources on completion, failure or cancellation.</summary>
    public async Task SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var client = CreateClient();
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a client without redirects, cookies or credentials only when a request is being sent.</summary>
    protected virtual HttpClient CreateClient() => new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        Credentials = null,
        ConnectTimeout = SendTimeout
    })
    { Timeout = SendTimeout };
}
